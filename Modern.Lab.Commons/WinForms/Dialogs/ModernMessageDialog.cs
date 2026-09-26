using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

using Modern.Lab.Controls.Wpf.Display;
using Modern.Lab.Controls.Wpf.Input;
using Modern.Lab.Theming;
using Modern.Lab.WinForms.Controls.Input;
using Modern.Lab.WinForms.Rendering;

namespace Modern.Lab.WinForms.Controls.Dialogs
{
    /// <summary>
    /// <see cref="MessageBox"/>의 모던 대체 — <b>정형화된 알림/확인 다이얼로그</b>다.
    ///
    /// <code>
    /// ModernMessageDialog.ShowError(this, "수신 실패", reply.Message);
    /// ModernMessageDialog.ShowInformation(this, "저장했습니다.");
    ///
    /// if (!ModernMessageDialog.Confirm(this, "삭제", "선택한 3건을 삭제할까요?")) { return; }
    /// </code>
    ///
    /// <b>토스트와 쓰는 자리가 다르다</b>: 토스트(<c>ShowToast</c>)는 <b>흘려도 되는</b>
    /// 짧은 성공/안내이고, 이 다이얼로그는 <b>사용자가 반드시 읽어야 하는</b> 실패
    /// 사유·확인 질문이다. 특히 서버 오류 문구는 길어서 배지·툴팁으로는 읽기
    /// 어려웠다(2026-08-04 ManualReceive 사례) — 여기서는 자동 줄바꿈 + 스크롤로
    /// 전문을 다 보여주고, 본문을 <b>드래그해 복사</b>할 수도 있다.
    ///
    /// 구성은 <b>본문만 WPF</b>다: <see cref="ModernMessageDialogControl"/>(아이콘 원 +
    /// 제목 + 본문)을 <see cref="ElementHost"/>로 얹고, <b>버튼은 WinForms
    /// <see cref="ModernButton"/></b>으로 둔다 — 그래야 폼의
    /// <see cref="Form.AcceptButton"/>/<see cref="Form.CancelButton"/>이 Enter/Esc를
    /// 관례대로 처리한다(ModernButton이 <c>IButtonControl</c>이라 가능해졌다).
    ///
    /// 창 크기는 본문 길이로 정한다 — 짧으면 작게, 길면 최대 높이까지 커지고 그
    /// 이상은 본문이 스크롤된다. 여는 쪽은 크기를 신경 쓰지 않는다.
    /// </summary>
    public sealed class ModernMessageDialog : Form
    {
        // ===== 치수 (Tokens.xaml 미러) =====

        // 기본 폭 — 한 줄 문구부터 서버 오류 전문까지 이 폭에서 읽힌다.
        private const int dialogWidth = 540;

        // 본문 영역 최대 높이 — 넘으면 본문이 스크롤된다(창이 화면을 넘지 않게).
        private const int maxBodyHeight = 190;

        // 본문 영역 최소 높이 — 한 줄 문구에서도 아이콘 원(40)과 균형이 맞게.
        private const int minBodyHeight = 120;

        // WPF 본문의 Padding(20,18,20,4) + 아이콘 원(40) + 아이콘 여백(16) 미러.
        private const int bodyPaddingLeft = 20;
        private const int bodyPaddingRight = 20;
        private const int bodyPaddingTop = 18;
        private const int bodyPaddingBottom = 4;
        private const int iconColumnWidth = 40 + 16;

        // 상단 헤더 — OS 제목줄을 쓰지 않으므로(구식으로 보인다) 닫기 버튼과
        // 드래그 손잡이만 있는 얇은 띠다. 제목은 본문이 이미 보여준다.
        private const int headerHeight = 34;
        private const int closeButtonSize = 30;

        // 창 테두리 1px — 제목줄이 없어 형태가 흐려지지 않게.
        private const int borderThickness = 1;

        // 버튼 줄 — 높이·버튼 크기·간격.
        private const int footerHeight = 60;
        private const int buttonWidth = 96;
        private const int buttonHeight = 32;
        private const int buttonGap = 8;
        private const int footerPadding = 20;

        // 상세 영역 — 접혀 있을 때는 자리를 차지하지 않고, 펼치면 창이 이 높이만큼
        // 아래로 자란다. 상세 박스는 본문 박스와 같은 생김새의 WPF
        // (ModernMessageDetailsControl)이고, 처음 펼칠 때 만든다 — 상세를 열지 않는
        // 오류 창에 WPF 섬 하나를 더 얹지 않기 위해서다.
        private const int detailsHeight = 168;
        private const int detailsButtonWidth = 104;

        private const string collapsedDetailsCaption = "Details";
        private const string expandedDetailsCaption = "Hide details";

        private readonly ElementHost host;
        private readonly Modern.Lab.Controls.Wpf.Display.ModernMessageDialogControl body;
        private readonly Panel footer;
        private readonly Panel header;
        private readonly ModernButton closeButton;
        private readonly Timer copyFeedbackTimer;
        private readonly string details;
        private Panel detailsPanel;
        private ElementHost detailsHost;
        private Modern.Lab.Controls.Wpf.Display.ModernMessageDetailsControl detailsBody;
        private ModernButton detailsButton;
        private bool detailsExpanded;

        /// <summary>
        /// 다이얼로그를 만든다 — <b>보통은 정적 진입점</b>
        /// (<see cref="ShowError"/>·<see cref="Confirm"/> 등)<b>을 쓴다.</b> 호출부가
        /// 크기·버튼 구성을 조립하지 않게 하는 것이 이 타입의 목적이므로, 직접 만드는
        /// 것은 창을 띄우지 않고 구성을 확인하는 자체 검사처럼 특별한 경우다.
        /// </summary>
        /// <param name="kind">메시지 종류.</param>
        /// <param name="title">제목 (null/빈 값이면 종류별 기본 제목).</param>
        /// <param name="message">본문.</param>
        /// <param name="buttons">버튼 구성.</param>
        public ModernMessageDialog(
                ModernMessageKind kind, string title, string message, MessageBoxButtons buttons)
            : this(kind, title, message, buttons, null)
        {
        }

        /// <summary>
        /// 기술 상세를 함께 갖는 다이얼로그 — 본문은 <b>현업이 읽고 행동할 수 있는
        /// 안내</b>이고, <paramref name="details"/>는 <b>개발자가 받아 볼 원문</b>이다
        /// (서버 응답 문구, 전문 이름, 예외 메시지). 상세는 접혀서 열리고
        /// "Details" 를 눌러야 보이며, 복사 버튼이 제목·본문·상세를 한 번에 담는다.
        ///
        /// 현업에게 내부 컬럼명이나 계약 용어를 보여 주지 않으면서도, "상세 눌러서
        /// 복사해 보내 주세요" 한 마디로 원문을 받을 수 있게 하는 것이 목적이다.
        /// </summary>
        /// <param name="kind">메시지 종류.</param>
        /// <param name="title">제목 (null/빈 값이면 종류별 기본 제목).</param>
        /// <param name="message">본문 — 사용자가 읽고 행동할 수 있는 말.</param>
        /// <param name="buttons">버튼 구성.</param>
        /// <param name="details">기술 상세 (null/빈 값이면 상세 버튼이 생기지 않는다).</param>
        public ModernMessageDialog(
                ModernMessageKind kind,
                string title,
                string message,
                MessageBoxButtons buttons,
                string details)
        {
            this.details = details == null ? string.Empty : details.Trim();
            this.body = new Modern.Lab.Controls.Wpf.Display.ModernMessageDialogControl();
            this.body.Kind = kind;
            this.body.Title = title ?? string.Empty;
            this.body.Message = message ?? string.Empty;
            this.body.CopyRequested += this.OnBodyCopyRequested;

            this.host = new ElementHost();
            this.host.Dock = DockStyle.Fill;
            this.host.Child = this.body;
            this.host.BackColorTransparent = false;
            this.host.BackColor = ModernTokenColors.Get("Brush.Surface", Color.White);

            this.footer = new Panel();
            this.footer.Dock = DockStyle.Bottom;

            // 폭을 미리 확정한다 — 버튼은 오른쪽 기준으로 배치하고 Anchor로
            // 고정하므로, 이 시점 폭이 최종 폭과 다르면 버튼이 밀린다.
            this.footer.Size = new Size(dialogWidth, footerHeight);
            this.footer.BackColor = ModernTokenColors.Get("Brush.Surface", Color.White);

            // 헤더 — 닫기 ✕ 하나 + 드래그. 클릭 결과는 Esc와 같게 만든다
            // (알림은 확인, 확인 창은 No). 그래서 닫기가 별도 정책이 되지 않는다.
            this.header = new Panel();
            this.header.Dock = DockStyle.Top;
            this.header.Size = new Size(dialogWidth, headerHeight);
            this.header.BackColor = ModernTokenColors.Get("Brush.Surface", Color.White);
            this.header.MouseDown += this.OnHeaderMouseDown;

            this.closeButton = new ModernButton();
            this.closeButton.Kind = ButtonKind.Subtle;
            this.closeButton.Text = string.Empty;
            this.closeButton.IconGlyph = "";
            this.closeButton.Size = new Size(closeButtonSize, closeButtonSize);
            this.closeButton.Location = new Point(
                    dialogWidth - closeButtonSize - 2, (headerHeight - closeButtonSize) / 2);
            this.closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.closeButton.TabStop = false;
            this.closeButton.Click += this.OnCloseClick;
            this.header.Controls.Add(this.closeButton);

            this.copyFeedbackTimer = new Timer();
            this.copyFeedbackTimer.Interval = 1200;
            this.copyFeedbackTimer.Tick += this.OnCopyFeedbackTick;

            this.SuspendLayout();
            this.InitializeShell(kind, title);
            this.InitializeButtons(buttons);
            this.InitializeDetails();

            // ★ Add 순서가 도킹 순서를 정한다 — WinForms는 Controls를 **뒤에서
            // 앞으로** 훑으며 자리를 배분하므로, **나중에 Add한 것이 먼저** 가장자리
            // 띠를 차지하고 Fill인 본문이 남은 영역을 가져간다. 뒤집으면 본문이
            // 전체를 차지해 버튼 줄과 겹친다(검사가 좌표로 잡는다).
            this.Controls.Add(this.host);
            this.Controls.Add(this.footer);

            if (this.detailsPanel != null)
            {
                this.Controls.Add(this.detailsPanel);
            }

            this.Controls.Add(this.header);

            int bodyHeight = this.MeasureBodyHeight();
            this.ClientSize = new Size(
                    dialogWidth + (borderThickness * 2),
                    headerHeight + bodyHeight + footerHeight + (borderThickness * 2));
            this.ResumeLayout(true);
        }

        // 창 자체의 설정.
        //
        // ★ <b>OS 제목줄을 쓰지 않는다</b>(FormBorderStyle.None) — 회색 캡션 + 시스템
        // 아이콘이 구식으로 보이고, 제목이 캡션과 본문에 **두 번** 나왔다.
        // 대신 얇은 헤더(닫기 ✕ + 드래그)를 우리가 그린다. Text는 그대로 둔다 —
        // Alt+Tab·접근성 도구가 창 이름으로 읽는다.
        //
        // 형태가 흐려지지 않게 Padding 1px + 테두리색 배경으로 **1px 프레임**을
        // 만든다(자식이 1px 안쪽으로 배치되어 바깥 1px만 남는다).
        private void InitializeShell(ModernMessageKind kind, string title)
        {
            this.Text = string.IsNullOrEmpty(title) ? DefaultCaption(kind) : title;
            this.FormBorderStyle = FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.AutoScaleMode = AutoScaleMode.None;
            this.BackColor = ModernTokenColors.Get("Brush.Border", Color.FromArgb(209, 209, 209));
            this.Padding = new Padding(borderThickness);
            this.KeyPreview = true;
        }

        // 헤더 드래그 — 제목줄이 없으므로 헤더를 캡션처럼 쓴다. 마우스 캡처를
        // 놓고 "캡션을 눌렀다"고 알리면 창 이동은 윈도우가 해 준다.
        private void OnHeaderMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            ReleaseCapture();
            SendMessage(this.Handle, WmNcLButtonDown, new IntPtr(HtCaption), IntPtr.Zero);
        }

        // 닫기 ✕ — Esc와 같은 결과를 낸다(알림은 확인, 확인 창은 No).
        private void OnCloseClick(object sender, EventArgs e)
        {
            if (this.CancelButton != null)
            {
                this.CancelButton.PerformClick();
                return;
            }

            this.DialogResult = DialogResult.Cancel;
        }

        private void OnBodyCopyRequested(object sender, EventArgs e)
        {
            this.CopyAllText();
        }

        /// <summary>
        /// 네이티브 MessageBox의 Ctrl+C 전체 복사 호환. 본문(WPF 읽기 전용 TextBox)에
        /// 포커스와 선택이 있으면 WPF가 먼저 처리해 여기 오지 않는다 — 선택 복사가
        /// 우선이고, 그 외(기본 포커스인 버튼 등)에서는 전체 복사다.
        /// </summary>
        /// <param name="msg">창 메시지.</param>
        /// <param name="keyData">키 조합.</param>
        /// <returns>처리했으면 true.</returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.C))
            {
                this.CopyAllText();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // 제목+본문 평문 복사 — 제목이 없으면 창 이름(종류별 기본 제목)을 쓴다.
        // 클립보드 잠금(원격 데스크톱·클립보드 관리 도구)으로 실패할 수 있으므로
        // 성공했을 때만 체크 피드백을 보여준다.
        private void CopyAllText()
        {
            if (Modern.Lab.WinForms.Controls.Hosting.SafeClipboard.TrySetText(this.ComposeCopyText()))
            {
                this.body.ShowCopyConfirmation();
                this.copyFeedbackTimer.Stop();
                this.copyFeedbackTimer.Start();
            }
        }

        private void OnCopyFeedbackTick(object sender, EventArgs e)
        {
            this.copyFeedbackTimer.Stop();
            this.body.ResetCopyConfirmation();

            if (this.detailsBody != null)
            {
                this.detailsBody.ResetCopyConfirmation();
            }
        }

        // 클립보드에 담는 평문 — 제목·본문에 상세가 있으면 이어 붙인다. 받는 쪽이
        // 개발자라 어느 화면 무슨 상황이었는지가 원문만큼 중요하다.
        private string ComposeCopyText()
        {
            string title = string.IsNullOrEmpty(this.body.Title) ? this.Text : this.body.Title;
            string message = Modern.Lab.Controls.Wpf.Display.ModernMessageDialogControl.StripEmphasis(
                    this.body.Message);
            string text = string.IsNullOrEmpty(title)
                    ? message
                    : title + Environment.NewLine + Environment.NewLine + message;

            if (this.details.Length == 0)
            {
                return text;
            }

            return text + Environment.NewLine + Environment.NewLine + this.details;
        }

        // 버튼 줄 — 오른쪽 정렬. 기본/취소 버튼을 폼에 지정해 Enter/Esc를 맡긴다.
        private void InitializeButtons(MessageBoxButtons buttons)
        {
            if (buttons == MessageBoxButtons.YesNo)
            {
                ModernButton no = this.CreateButton("No", ButtonKind.Secondary, DialogResult.No);
                ModernButton yes = this.CreateButton("Yes", ButtonKind.Primary, DialogResult.Yes);

                this.PlaceButtons(new ModernButton[] { yes, no });
                this.AcceptButton = yes;
                this.CancelButton = no;
                return;
            }

            if (buttons == MessageBoxButtons.OKCancel)
            {
                ModernButton cancel = this.CreateButton(
                        "Cancel", ButtonKind.Secondary, DialogResult.Cancel);
                ModernButton ok = this.CreateButton("OK", ButtonKind.Primary, DialogResult.OK);

                this.PlaceButtons(new ModernButton[] { ok, cancel });
                this.AcceptButton = ok;
                this.CancelButton = cancel;
                return;
            }

            // 그 밖에는 확인 하나 — Enter/Esc 모두 닫는다(알림 성격).
            ModernButton close = this.CreateButton("OK", ButtonKind.Primary, DialogResult.OK);

            this.PlaceButtons(new ModernButton[] { close });
            this.AcceptButton = close;
            this.CancelButton = close;
        }

        private ModernButton CreateButton(string caption, ButtonKind kind, DialogResult result)
        {
            ModernButton button = new ModernButton();
            button.Text = caption;
            button.Kind = kind;
            button.DialogResult = result;
            button.Size = new Size(buttonWidth, buttonHeight);
            return button;
        }

        // 오른쪽 끝에서부터 순서대로 붙인다 (첫 항목이 가장 오른쪽 = 기본 동작).
        private void PlaceButtons(ModernButton[] buttons)
        {
            int right = dialogWidth - footerPadding;
            int top = (footerHeight - buttonHeight) / 2;

            for (int i = 0; i < buttons.Length; i = i + 1)
            {
                buttons[i].Location = new Point(right - buttons[i].Width, top);
                buttons[i].Anchor = AnchorStyles.Top | AnchorStyles.Right;
                this.footer.Controls.Add(buttons[i]);
                right = right - buttons[i].Width - buttonGap;
            }
        }

        // 상세 영역 — 상세 문구가 없으면 아무것도 만들지 않는다(버튼도 안 생긴다).
        //
        // 자리는 클래식하게 잡는다: 버튼 줄 **왼쪽 끝**에 "Details", 펼치면 버튼 줄
        // **아래로** 창이 자란다. 생김새는 토큰을 따르므로 테마가 바뀌면 같이 바뀐다.
        private void InitializeDetails()
        {
            if (this.details.Length == 0)
            {
                return;
            }

            this.detailsButton = new ModernButton();
            this.detailsButton.Kind = ButtonKind.Subtle;
            this.detailsButton.Text = collapsedDetailsCaption;
            this.detailsButton.Size = new Size(detailsButtonWidth, buttonHeight);
            this.detailsButton.Location = new Point(footerPadding, (footerHeight - buttonHeight) / 2);
            this.detailsButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.detailsButton.Click += this.OnDetailsToggleClick;
            this.footer.Controls.Add(this.detailsButton);

            // 폭을 미리 확정한다 — 기본 폭(200)으로 두면 도킹으로 540까지 늘어날 때
            // 안쪽 요소가 그만큼 밀려 오른쪽이 잘린다(버튼 줄과 같은 이유).
            this.detailsPanel = new Panel();
            this.detailsPanel.Dock = DockStyle.Bottom;
            this.detailsPanel.Size = new Size(dialogWidth, detailsHeight);
            this.detailsPanel.Visible = false;
            this.detailsPanel.BackColor = ModernTokenColors.Get("Brush.Surface", Color.White);
        }

        // 상세 박스를 처음 펼칠 때 만든다. 패널을 채우므로 창 폭을 그대로 따른다.
        private void EnsureDetailsBody()
        {
            if (this.detailsHost != null)
            {
                return;
            }

            this.detailsBody = new Modern.Lab.Controls.Wpf.Display.ModernMessageDetailsControl();
            this.detailsBody.Details = this.details;
            this.detailsBody.CopyRequested += this.OnDetailsCopyRequested;

            this.detailsHost = new ElementHost();
            this.detailsHost.Dock = DockStyle.Fill;
            this.detailsHost.BackColorTransparent = false;
            this.detailsHost.BackColor = ModernTokenColors.Get("Brush.Surface", Color.White);
            this.detailsHost.Child = this.detailsBody;
            this.detailsPanel.Controls.Add(this.detailsHost);
        }

        // 펼침/접힘 — 본문 크기는 그대로 두고 창만 아래로 자라게, 보임 전환과
        // 높이 변경을 한 레이아웃 안에서 함께 한다(따로 하면 한 프레임 찌그러진다).
        private void OnDetailsToggleClick(object sender, EventArgs e)
        {
            this.detailsExpanded = !this.detailsExpanded;

            this.SuspendLayout();

            if (this.detailsExpanded)
            {
                this.EnsureDetailsBody();
            }

            this.detailsPanel.Visible = this.detailsExpanded;
            this.Height = this.Height + (this.detailsExpanded ? detailsHeight : -detailsHeight);
            this.detailsButton.Text = this.detailsExpanded
                    ? expandedDetailsCaption
                    : collapsedDetailsCaption;
            this.ResumeLayout(true);
        }

        // 상세 복사 — 현업이 눌러 개발자에게 보내는 버튼이므로 제목·본문·상세를
        // 모두 담는다(상세만 보내면 어느 화면 무슨 상황인지 알 수 없다).
        private void OnDetailsCopyRequested(object sender, EventArgs e)
        {
            if (!Modern.Lab.WinForms.Controls.Hosting.SafeClipboard.TrySetText(this.ComposeCopyText()))
            {
                return;
            }

            this.detailsBody.ShowCopyConfirmation();
            this.copyFeedbackTimer.Stop();
            this.copyFeedbackTimer.Start();
        }

        // 본문 높이 — 제목/본문을 실제 폭으로 줄바꿈해 재어 창 높이를 정한다.
        // WPF는 DIU, GDI는 픽셀이라 완전히 같지는 않지만 창 크기 결정에는 충분하다
        // (모자라면 본문이 스크롤되고, 남으면 아래 여백이 조금 생긴다).
        private int MeasureBodyHeight()
        {
            int textWidth = dialogWidth
                    - bodyPaddingLeft - bodyPaddingRight - iconColumnWidth - 60;
            const TextFormatFlags flags = TextFormatFlags.WordBreak
                    | TextFormatFlags.NoPadding
                    | TextFormatFlags.NoPrefix;
            int height = 0;

            if (!string.IsNullOrEmpty(this.body.Title))
            {
                using (Font titleFont = ModernFonts.Create(
                        ModernFonts.TitleDiu, true, FontStyle.Regular))
                {
                    height = height + TextRenderer.MeasureText(
                            this.body.Title,
                            titleFont,
                            new Size(textWidth, int.MaxValue),
                            flags).Height + 6;
                }
            }

            if (!string.IsNullOrEmpty(this.body.Message))
            {
                using (Font bodyFont = ModernFonts.Create(
                        ModernFonts.BodyDiu, false, FontStyle.Regular))
                {
                    height = height + TextRenderer.MeasureText(
                            Modern.Lab.Controls.Wpf.Display.ModernMessageDialogControl.StripEmphasis(this.body.Message),
                            bodyFont,
                            new Size(textWidth, int.MaxValue),
                            flags).Height;
                }
            }

            height = height + bodyPaddingTop + bodyPaddingBottom + 22;

            if (height < minBodyHeight + bodyPaddingTop + bodyPaddingBottom)
            {
                height = minBodyHeight + bodyPaddingTop + bodyPaddingBottom;
            }

            if (height > maxBodyHeight)
            {
                height = maxBodyHeight;
            }

            return height;
        }

        /// <summary>
        /// 창 모서리를 둥글게 한다 (Windows 11 DWM). 지원하지 않는 OS에서는
        /// 호출이 조용히 실패하고 각진 모서리로 남는다 — 기능에는 영향이 없다.
        /// </summary>
        /// <param name="e">이벤트 인자.</param>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            int preference = CornerPreferenceRound;

            try
            {
                DwmSetWindowAttribute(
                        this.Handle, DwmWindowCornerPreference, ref preference, sizeof(int));
            }
            catch (EntryPointNotFoundException)
            {
                // 구형 OS — 둥근 모서리만 없다.
            }
            catch (DllNotFoundException)
            {
                // dwmapi.dll이 없는 환경 — 같다.
            }
        }

        // ===== Win32 =====

        private const int WmNcLButtonDown = 0x00A1;
        private const int HtCaption = 2;

        // DWMWA_WINDOW_CORNER_PREFERENCE(33) = DWMWCP_ROUND(2) — Windows 11.
        private const int DwmWindowCornerPreference = 33;
        private const int CornerPreferenceRound = 2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(
                IntPtr window, int attribute, ref int value, int size);

        // ===== 정적 진입점 (MessageBox와 같은 자리) =====

        /// <summary>정보 알림 (확인 버튼 하나).</summary>
        /// <param name="owner">부모 창 (null 허용).</param>
        /// <param name="message">본문.</param>
        public static void ShowInformation(IWin32Window owner, string message)
        {
            Show(owner, ModernMessageKind.Information, null, message, MessageBoxButtons.OK);
        }

        /// <summary>제목까지 지정하는 정보 알림.</summary>
        /// <param name="owner">부모 창 (null 허용).</param>
        /// <param name="title">제목.</param>
        /// <param name="message">본문.</param>
        public static void ShowInformation(IWin32Window owner, string title, string message)
        {
            Show(owner, ModernMessageKind.Information, title, message, MessageBoxButtons.OK);
        }

        /// <summary>성공 알림 — 처리 결과를 반드시 확인시켜야 할 때만 쓴다
        /// (가벼운 성공은 토스트가 맞다).</summary>
        /// <param name="owner">부모 창 (null 허용).</param>
        /// <param name="title">제목.</param>
        /// <param name="message">본문.</param>
        public static void ShowSuccess(IWin32Window owner, string title, string message)
        {
            Show(owner, ModernMessageKind.Success, title, message, MessageBoxButtons.OK);
        }

        /// <summary>경고 알림.</summary>
        /// <param name="owner">부모 창 (null 허용).</param>
        /// <param name="title">제목.</param>
        /// <param name="message">본문.</param>
        public static void ShowWarning(IWin32Window owner, string title, string message)
        {
            Show(owner, ModernMessageKind.Warning, title, message, MessageBoxButtons.OK);
        }

        /// <summary>오류 알림 — 서버 실패 사유처럼 긴 문구를 그대로 넘긴다.</summary>
        /// <param name="owner">부모 창 (null 허용).</param>
        /// <param name="title">제목.</param>
        /// <param name="message">본문.</param>
        public static void ShowError(IWin32Window owner, string title, string message)
        {
            Show(owner, ModernMessageKind.Error, title, message, MessageBoxButtons.OK);
        }

        /// <summary>
        /// 기술 상세를 접어 둔 오류 알림 — <paramref name="message"/>는 <b>현업이 읽고
        /// 행동할 수 있는 안내</b>로 쓰고, 서버 응답 원문·전문 이름·예외 문구는
        /// <paramref name="details"/>로 넘긴다. 현업은 안내만 보고, 필요하면
        /// "Details → Copy" 로 원문을 통째로 복사해 개발자에게 보낼 수 있다.
        /// </summary>
        /// <param name="owner">부모 창 (null 허용).</param>
        /// <param name="title">제목.</param>
        /// <param name="message">본문 — 사용자가 읽고 행동할 수 있는 말.</param>
        /// <param name="details">기술 상세 (null/빈 값이면 상세 버튼이 생기지 않는다).</param>
        public static void ShowError(
                IWin32Window owner, string title, string message, string details)
        {
            Show(owner, ModernMessageKind.Error, title, message, MessageBoxButtons.OK, details);
        }

        /// <summary>
        /// 예/아니오 확인 — 되돌릴 수 없는 처리 전에 쓴다. 기본 버튼은 Yes이고
        /// Esc는 No다.
        /// </summary>
        /// <param name="owner">부모 창 (null 허용).</param>
        /// <param name="title">제목.</param>
        /// <param name="message">본문.</param>
        /// <returns>Yes를 눌렀으면 true.</returns>
        public static bool Confirm(IWin32Window owner, string title, string message)
        {
            return Show(owner, ModernMessageKind.Question, title, message, MessageBoxButtons.YesNo)
                    == DialogResult.Yes;
        }

        /// <summary>
        /// 본문 안에서 <b>강조</b>할 낱말을 감싼다 — 액센트색 SemiBold로 그려진다
        /// (2026-08-29 추가). "OffLine → OnLineLocal"처럼 <b>무엇으로 바꾸는가</b>를 눈에
        /// 띄게 할 때: <c>Confirm(this, "Confirm", "Change " + id + " to " + Emphasis(mode) + "?")</c>.
        /// 복사·높이 계산은 표기를 걷어낸 순수 본문을 쓴다.
        /// </summary>
        public static string Emphasis(string text)
        {
            return Modern.Lab.Controls.Wpf.Display.ModernMessageDialogControl.EmphasisMarker
                    + (text ?? string.Empty)
                    + Modern.Lab.Controls.Wpf.Display.ModernMessageDialogControl.EmphasisMarker;
        }

        /// <summary>
        /// 종류·버튼 구성을 직접 고르는 진입점.
        /// <paramref name="buttons"/>는 <see cref="MessageBoxButtons.OK"/>/
        /// <see cref="MessageBoxButtons.OKCancel"/>/<see cref="MessageBoxButtons.YesNo"/>를
        /// 지원하고, 그 밖의 값은 확인 하나로 처리한다.
        /// </summary>
        /// <param name="owner">부모 창 (null 허용).</param>
        /// <param name="kind">메시지 종류.</param>
        /// <param name="title">제목 (null/빈 값이면 종류별 기본 제목).</param>
        /// <param name="message">본문.</param>
        /// <param name="buttons">버튼 구성.</param>
        /// <returns>누른 버튼의 <see cref="DialogResult"/>.</returns>
        public static DialogResult Show(
                IWin32Window owner,
                ModernMessageKind kind,
                string title,
                string message,
                MessageBoxButtons buttons)
        {
            return Show(owner, kind, title, message, buttons, null);
        }

        /// <summary>
        /// 기술 상세를 함께 넘기는 진입점 — 상세가 있으면 버튼 줄 왼쪽에 "Details"가
        /// 생기고, 펼치면 원문과 복사 버튼이 나온다.
        /// </summary>
        /// <param name="owner">부모 창 (null 허용).</param>
        /// <param name="kind">메시지 종류.</param>
        /// <param name="title">제목 (null/빈 값이면 종류별 기본 제목).</param>
        /// <param name="message">본문 — 사용자가 읽고 행동할 수 있는 말.</param>
        /// <param name="buttons">버튼 구성.</param>
        /// <param name="details">기술 상세 (null/빈 값이면 상세 버튼이 생기지 않는다).</param>
        /// <returns>누른 버튼의 <see cref="DialogResult"/>.</returns>
        public static DialogResult Show(
                IWin32Window owner,
                ModernMessageKind kind,
                string title,
                string message,
                MessageBoxButtons buttons,
                string details)
        {
            using (ModernMessageDialog dialog =
                    new ModernMessageDialog(kind, title, message, buttons, details))
            {
                return owner == null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
            }
        }

        // 제목을 주지 않았을 때 창 제목줄에 쓸 기본 문구 — 표시 언어는 영어가
        // 기본이다(CLAUDE.md 규칙). 한국어 화면은 title을 넘겨 쓴다.
        private static string DefaultCaption(ModernMessageKind kind)
        {
            switch (kind)
            {
                case ModernMessageKind.Success:
                    return "Success";

                case ModernMessageKind.Warning:
                    return "Warning";

                case ModernMessageKind.Error:
                    return "Error";

                case ModernMessageKind.Question:
                    return "Confirm";

                default:
                    return "Information";
            }
        }

        /// <summary>ElementHost와 WPF 본문을 함께 정리한다.</summary>
        /// <param name="disposing">관리 자원 해제 여부.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.host != null)
                {
                    this.body.CopyRequested -= this.OnBodyCopyRequested;
                    this.host.Child = null;
                    this.host.Dispose();
                }

                if (this.detailsHost != null)
                {
                    this.detailsBody.CopyRequested -= this.OnDetailsCopyRequested;
                    this.detailsHost.Child = null;
                    this.detailsHost.Dispose();
                }

                if (this.copyFeedbackTimer != null)
                {
                    this.copyFeedbackTimer.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
