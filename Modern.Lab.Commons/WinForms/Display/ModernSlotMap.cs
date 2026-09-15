using System;
using System.ComponentModel;
using Modern.Lab.Controls.Wpf.Display;
using Modern.Lab.WinForms.Controls.Hosting;

namespace Modern.Lab.WinForms.Controls.Display
{
    /// <summary>
    /// ModernSlotMapControl(WPF)의 WinForms 래퍼 — 캐리어 수납 구조를 실물
    /// 단면처럼 그리는 슬롯 맵.
    ///
    /// 사용 흐름: 폼이 SlotMapSection[](구획/셀 모델)을 만들어 SetSections로
    /// 통째로 주고(재조회 = 재구성), 채워진 셀과 미리보기 셀의 클릭 키를 CellClicked로 받는다.
    /// 폼은 SetClickKeys와 SetSelectedKeys로 클릭 선택과 스테이징을 구분한다.
    /// 보기 전용 맵은 AllowSelection을 끄고
    /// SetPreview(구획별 웨이퍼 수)로 "들어갈 빈 자리" 하이라이트를 켠다.
    /// </summary>
    [ToolboxItem(true)]
    public class ModernSlotMap : WpfElementHostBase<ModernSlotMapControl>
    {
        // 디자인 타임 WPF 생성 실패 시에도 속성 그리드가 동작하는 폴백 저장소.
        private bool fallbackAllowSelection = true;
        private bool fallbackEnableDragOut;
        private bool fallbackAcceptDrops;

        /// <summary>선택이 바뀔 때 발생한다 (재구성/프로그램 선택 해제 등).</summary>
        public event EventHandler SelectionChanged;

        /// <summary>채움 셀 또는 미리보기 셀 클릭 시 발생한다 — 선택 상태는 바꾸지 않고 클릭된
        /// 키만 알린다. 선택 표시는 폼이 SetClickKeys로 관리한다.</summary>
        public event EventHandler<SlotMapCellEventArgs> CellClicked;

        /// <summary>드롭을 받을 때 발생한다 (AcceptDrops = true) — 끌려온 셀
        /// 키들과 놓은 자리(앵커) 셀 키. 검증/이동은 폼이 서버 호출로 한다.</summary>
        public event EventHandler<SlotMapDropEventArgs> WafersDropped;

        /// <summary>맵에서 오른쪽 클릭할 때 발생한다 — 커서 아래 채움 셀 키
        /// (셀 밖이면 빈 문자열). 폼이 이동 컨텍스트 메뉴를 띄운다.</summary>
        public event EventHandler<SlotMapCellEventArgs> CellRightClick;

        public ModernSlotMap()
        {
            if (this.Wpf != null)
            {
                this.Wpf.SelectionChanged += this.OnInnerSelectionChanged;
                this.Wpf.CellClicked += this.OnInnerCellClicked;
                this.Wpf.WafersDropped += this.OnInnerWafersDropped;
                this.Wpf.CellRightClick += this.OnInnerCellRightClick;
            }
        }

        /// <summary>셀 클릭 선택 허용 여부 (기본 true) — 대상(읽기 전용)
        /// 맵은 false로 둔다.</summary>
        [Category("Modern")]
        [Description("Allow selecting cells by click — false for target (read-only) maps")]
        [DefaultValue(true)]
        public bool AllowSelection
        {
            get
            {
                return this.Wpf != null ? this.Wpf.AllowSelection : this.fallbackAllowSelection;
            }
            set
            {
                this.fallbackAllowSelection = value;

                if (this.Wpf != null)
                {
                    this.Wpf.AllowSelection = value;
                }

                this.InvalidateDesignTimePreview();
            }
        }

        /// <summary>채워진 셀 드래그 시작 허용 (기본 false) — 원본 맵에서 켠다.
        /// 선택된 셀을 끌면 선택 전체가 함께 간다.</summary>
        [Category("Modern")]
        [Description("Allow dragging filled cells — enable on the source map")]
        [DefaultValue(false)]
        public bool EnableDragOut
        {
            get
            {
                return this.Wpf != null ? this.Wpf.EnableDragOut : this.fallbackEnableDragOut;
            }
            set
            {
                this.fallbackEnableDragOut = value;

                if (this.Wpf != null)
                {
                    this.Wpf.EnableDragOut = value;
                }
            }
        }

        /// <summary>드롭 수용 (기본 false) — 대상 맵에서 켜면 WafersDropped가
        /// 발생한다. 놓은 셀이 앵커("이 자리부터 채움")로 전달된다.</summary>
        [Category("Modern")]
        [Description("Accept drops — enable on the target map (raises WafersDropped)")]
        [DefaultValue(false)]
        public bool AcceptDrops
        {
            get
            {
                return this.Wpf != null ? this.Wpf.AcceptDrops : this.fallbackAcceptDrops;
            }
            set
            {
                this.fallbackAcceptDrops = value;

                if (this.Wpf != null)
                {
                    this.Wpf.AcceptDrops = value;
                }
            }
        }

        /// <summary>현재 선택된 셀 키 목록 (없으면 빈 배열).</summary>
        [Browsable(false)]
        public string[] SelectedKeys
        {
            get { return this.Wpf != null ? this.Wpf.SelectedKeys : new string[0]; }
        }

        /// <summary>구획들을 통째로 다시 그린다 — 재조회 반영 경로.</summary>
        public void SetSections(SlotMapSection[] sections)
        {
            if (this.Wpf != null)
            {
                this.Wpf.SetSections(sections);
            }
        }

        /// <summary>선택을 모두 해제한다.</summary>
        public void ClearSelection()
        {
            if (this.Wpf != null)
            {
                this.Wpf.ClearSelection();
            }
        }

        /// <summary>지정 키들만 선택 상태로 만든다 (이벤트 없음) — 이동 후
        /// 도착지에서 방금 옮긴 웨이퍼을 강조할 때 쓴다.</summary>
        public void SetSelectedKeys(string[] keys)
        {
            if (this.Wpf != null)
            {
                this.Wpf.SetSelectedKeys(keys);
            }
        }

        /// <summary>클릭 강조 셀을 지정한다 (스테이징과 다른 색; null이면 없음).</summary>
        public void SetClickKey(string key)
        {
            if (this.Wpf != null)
            {
                this.Wpf.SetClickKey(key);
            }
        }

        /// <summary>클릭 강조 셀들을 지정한다. 채움 색을 바꾸지 않고 고대비
        /// 경고색 외곽선과 같은 색 슬롯 번호 배지를 표시한다.</summary>
        public void SetClickKeys(string[] keys)
        {
            if (this.Wpf != null)
            {
                this.Wpf.SetClickKeys(keys);
            }
        }

        /// <summary>자리 키("SLOT|7"/"STUB|3"/"LCC|3|A") → 들어올 웨이퍼 ID
        /// 미리보기 맵 (null = 해제) — 그 자리가 비어 있으면 "→ ID"로 표기되고
        /// 번호 칩이 하이라이트된다. 서버 배치 계획을 그대로 받아 주므로
        /// 미리보기와 실제 이동 결과가 일치한다.</summary>
        public void SetPreview(System.Collections.Generic.Dictionary<string, string> map)
        {
            if (this.Wpf != null)
            {
                this.Wpf.SetPreview(map);
            }
        }

        /// <summary>LCC 미리보기의 자리 키 → 삽입 위치(Top/Left/Right) 맵.
        /// 비어 있는 대상 핑거에도 이동 후의 삽입 방향을 표시한다.</summary>
        public void SetPreviewMarkers(System.Collections.Generic.Dictionary<string, string> markers)
        {
            if (this.Wpf != null)
            {
                this.Wpf.SetPreviewMarkers(markers);
            }
        }

        private void OnInnerSelectionChanged(object sender, EventArgs e)
        {
            EventHandler handler = this.SelectionChanged;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void OnInnerCellClicked(object sender, SlotMapCellEventArgs e)
        {
            EventHandler<SlotMapCellEventArgs> handler = this.CellClicked;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnInnerWafersDropped(object sender, SlotMapDropEventArgs e)
        {
            EventHandler<SlotMapDropEventArgs> handler = this.WafersDropped;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnInnerCellRightClick(object sender, SlotMapCellEventArgs e)
        {
            EventHandler<SlotMapCellEventArgs> handler = this.CellRightClick;

            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}
