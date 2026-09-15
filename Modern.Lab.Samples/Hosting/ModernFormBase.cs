using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;

using Modern.Lab.Theming;

namespace Modern.Lab.Hosting
{

    public partial class ModernFormBase : Form
    {
        private readonly Dictionary<string, int> loadVersions = new Dictionary<string, int>();

        private Modern.Lab.WinForms.Controls.Display.ModernToast sharedToast;

        private Label noticeLine;
        private Modern.Lab.WinForms.Controls.Display.ModernBusyOverlay sharedBusyOverlay;

        private bool connectionLost;

        private bool failureNoticeShowing;

        private bool stickyNoticeShowing;

        private bool loadFailureSilent;

        private string contractNoticeText = string.Empty;

        private string renderedNoticeBasis = string.Empty;

        private readonly List<string> pendingFailureTexts = new List<string>();

        private readonly Dictionary<string, DateTime> lastLoadSuccessTimes =
                new Dictionary<string, DateTime>(StringComparer.Ordinal);
        private readonly HashSet<string> staleChannels =
                new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> silentFailureChannels =
                new HashSet<string>(StringComparer.Ordinal);
        private DateTime lastAnyLoadSuccessTime;

        private const string failureDialogKey = "modernformbase.load-failure";

        private readonly Dictionary<string, System.Windows.Forms.Timer> debounceTimers =
                new Dictionary<string, System.Windows.Forms.Timer>();

        protected void InitializeModernForm(bool useLoadCover = true)
        {
            if (useLoadCover)
            {
                ModernLoadCover.Attach(this);
            }

            this.EnsureFormInitialized();
        }

        protected void InitializeModernForm(Control coverTarget)
        {
            this.InitializeModernForm(coverTarget, true);
        }

        protected void InitializeModernForm(Control coverTarget, bool autoReleaseOnIdle)
        {
            this.InitializeModernForm(coverTarget, autoReleaseOnIdle, ModernLoadCover.DefaultMessage);
        }

        protected void InitializeModernForm(
                Control coverTarget, bool autoReleaseOnIdle, string coverMessage)
        {
            ModernLoadCover.Attach(this, coverTarget, coverMessage, autoReleaseOnIdle);
            this.EnsureFormInitialized();
        }

        protected void InitializeModernFormWithMessage(string coverMessage)
        {
            ModernLoadCover.Attach(this, null, coverMessage, true);
            this.EnsureFormInitialized();
        }

        protected void ReleaseLoadCover()
        {
            ModernLoadCover.Release(this);
        }

        private readonly List<Modern.Lab.WinForms.Controls.Data.ModernDataGrid> findShortcutGrids =
                new List<Modern.Lab.WinForms.Controls.Data.ModernDataGrid>();

        private Modern.Lab.WinForms.Controls.Data.ModernDataGrid lastFocusedFindGrid;

        protected void RegisterFindShortcut(
                params Modern.Lab.WinForms.Controls.Data.ModernDataGrid[] grids)
        {
            if (grids == null)
            {
                return;
            }

            foreach (Modern.Lab.WinForms.Controls.Data.ModernDataGrid grid in grids)
            {
                if (grid == null)
                {
                    continue;
                }

                Modern.Lab.WinForms.Controls.Data.ModernDataGrid captured = grid;
                this.findShortcutGrids.Add(captured);

                captured.Enter += delegate { this.lastFocusedFindGrid = captured; };
            }
        }

        private Modern.Lab.WinForms.Controls.Data.ModernDataGrid ResolveFindShortcutGrid()
        {
            if (this.lastFocusedFindGrid != null
                    && this.lastFocusedFindGrid.Visible
                    && this.lastFocusedFindGrid.AllowFindPanel)
            {
                return this.lastFocusedFindGrid;
            }

            foreach (Modern.Lab.WinForms.Controls.Data.ModernDataGrid grid in this.findShortcutGrids)
            {
                if (grid.Visible && grid.AllowFindPanel)
                {
                    return grid;
                }
            }

            return null;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F))
            {
                Modern.Lab.WinForms.Controls.Data.ModernDataGrid target =
                        this.ResolveFindShortcutGrid();

                if (target != null)
                {
                    target.ShowFindPanel();
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private bool deferredResizeActive;

        [Category("Modern")]
        [Description("Defer child layout while dragging the window border and apply once on release (recommended for screens with many WPF islands)")]
        [DefaultValue(false)]
        public bool DeferredResize { get; set; }

        protected override void OnResizeBegin(EventArgs e)
        {
            base.OnResizeBegin(e);

            if (this.DeferredResize && !this.deferredResizeActive)
            {
                this.deferredResizeActive = true;
                this.SuspendLayout();
            }
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            if (this.deferredResizeActive)
            {
                this.deferredResizeActive = false;
                this.ResumeLayout(true);
            }

            base.OnResizeEnd(e);
        }

        private bool formInitialized;

        private void EnsureFormInitialized()
        {
            if (this.formInitialized)
            {
                return;
            }

            if (IsDesignTime())
            {
                return;
            }

            this.formInitialized = true;
            this.InitializeMessaging();
            this.ApplyThemeIfEnabled();
        }

        protected static bool IsDesignTime()
        {
            return System.ComponentModel.LicenseManager.UsageMode
                    == System.ComponentModel.LicenseUsageMode.Designtime;
        }

        protected virtual bool ApplyModernTheme
        {
            get { return true; }
        }

        private void ApplyThemeIfEnabled()
        {
            if (IsDesignTime() || this.DesignMode || !this.ApplyModernTheme)
            {
                return;
            }

            this.ApplyTheme();
        }

        protected virtual void ApplyTheme()
        {
            ModernThemeWinForms.Apply(this);
        }


        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (IsDesignTime() || this.DesignMode)
            {
                return;
            }

            this.EnsureFormInitialized();
        }








        protected override void OnLoad(EventArgs e)
        {
            this.ApplyThemeIfEnabled();

            base.OnLoad(e);
        }

        protected virtual void InitializeMessaging()
        {
        }

        protected virtual void DisposeMessaging()
        {
        }


        protected void ShowToast(string message)
        {
            this.ShowToast(message, Modern.Lab.Controls.Wpf.Display.ToastKind.Info);
        }







        protected void ShowToast(string message, Modern.Lab.Controls.Wpf.Display.ToastKind kind)
        {
            this.RecordNotice(message, kind);

            if (kind == Modern.Lab.Controls.Wpf.Display.ToastKind.Error)
            {
                this.ShowErrorMessage(this.NoticeErrorCaption, message);
                return;
            }

            if (kind == Modern.Lab.Controls.Wpf.Display.ToastKind.Warning)
            {
                this.ShowMessage(
                        Modern.Lab.Controls.Wpf.Display.ModernMessageKind.Warning,
                        this.NoticeWarningCaption,
                        message);
                return;
            }

            this.ShowTransientNotice(message, kind);
        }






        protected virtual void ShowStickyNotice(
                string message, Modern.Lab.Controls.Wpf.Display.ToastKind kind)
        {
            this.ShowNotice(message, kind, true);
        }





        protected virtual void ShowTransientNotice(
                string message, Modern.Lab.Controls.Wpf.Display.ToastKind kind)
        {
            if (this.stickyNoticeShowing && this.sharedToast != null && this.sharedToast.Visible
                    && (this.staleChannels.Count > 0 || this.contractNoticeText.Length > 0))
            {
                return;
            }

            this.ShowNotice(message, kind, false);
        }


        protected virtual void HideNotice()
        {
            this.stickyNoticeShowing = false;

            if (this.sharedToast != null)
            {
                this.sharedToast.HideToast();
            }
        }






        protected void SetContractNotice(string text)
        {
            this.contractNoticeText = text ?? string.Empty;
            this.RenderStickyNotice();
        }

        private string NoticeBasis()
        {
            List<string> stale = new List<string>(this.silentFailureChannels);
            stale.Sort(StringComparer.Ordinal);

            return string.Join(",", stale.ToArray())
                    + "|" + this.contractNoticeText;
        }

        private void RenderStickyNotice()
        {
            string basis = this.NoticeBasis();

            if (basis == this.renderedNoticeBasis)
            {
                return;
            }

            this.renderedNoticeBasis = basis;

            if (this.silentFailureChannels.Count > 0)
            {
                this.ShowStickyNotice(
                        this.BuildStaleNoticeText(this.StaleLeadText(this.silentFailureChannels.Count), this.silentFailureChannels)
                                + " " + this.RetryingText,
                        Modern.Lab.Controls.Wpf.Display.ToastKind.Error);
                return;
            }

            if (this.contractNoticeText.Length > 0)
            {
                this.ShowStickyNotice(
                        this.contractNoticeText,
                        Modern.Lab.Controls.Wpf.Display.ToastKind.Warning);
                return;
            }

            this.HideNotice();
        }





        protected virtual string StaleLeadText(int staleCount)
        {
            return staleCount == 1
                    ? "1 data set not refreshed"
                    : staleCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                            + " data sets not refreshed";
        }


        protected virtual string NoticeErrorCaption
        {
            get { return "Error"; }
        }


        protected virtual string NoticeWarningCaption
        {
            get { return "Warning"; }
        }


        protected virtual string QueryFailedCaption
        {
            get { return "Query failed"; }
        }


        protected virtual string RetryingText
        {
            get { return "Retrying automatically…"; }
        }

        /// <summary>
        /// 마지막 알림 한 줄 — 토스트는 지나가지만 이 줄은 남는다. 눈을 돌렸다 와도
        /// 방금 무슨 일이 있었는지 읽을 수 있어야 한다(2026-09-14 현장 요청).
        /// 실패·거절은 빨강, 그 밖은 평범한 글자색이다.
        /// </summary>
        protected void RecordNotice(string message, Modern.Lab.Controls.Wpf.Display.ToastKind kind)
        {
            if (this.IsDisposed || this.Disposing || message == null || message.Trim().Length == 0)
            {
                return;
            }

            if (this.noticeLine == null)
            {
                this.noticeLine = new Label();
                this.noticeLine.Dock = DockStyle.Bottom;
                this.noticeLine.Height = 24;
                this.noticeLine.Padding = new Padding(10, 0, 10, 0);
                this.noticeLine.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                this.noticeLine.AutoEllipsis = true;
                this.noticeLine.UseMnemonic = false;
                this.Controls.Add(this.noticeLine);
                this.noticeLine.BringToFront();
            }

            bool failed = kind == Modern.Lab.Controls.Wpf.Display.ToastKind.Error
                    || kind == Modern.Lab.Controls.Wpf.Display.ToastKind.Warning;

            this.noticeLine.ForeColor = failed
                    ? Modern.Lab.Theming.ModernTokenColors.Get("Brush.DangerText", System.Drawing.Color.Firebrick)
                    : Modern.Lab.Theming.ModernTokenColors.Get("Brush.TextSecondary", System.Drawing.Color.DimGray);
            this.noticeLine.Text = DateTime.Now.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture)
                    + "  " + message.Trim();
        }

        private void ShowNotice(
                string message, Modern.Lab.Controls.Wpf.Display.ToastKind kind, bool sticky)
        {
            if (this.IsDisposed || this.Disposing)
            {
                return;
            }

            if (this.sharedToast == null)
            {
                this.sharedToast = new Modern.Lab.WinForms.Controls.Display.ModernToast();
                this.Controls.Add(this.sharedToast);
            }

            this.RecordNotice(message, kind);
            this.stickyNoticeShowing = sticky;
            this.sharedToast.BringToFront();
            this.sharedToast.Show(message, kind, sticky);
        }

        private int queryDepth;

        private bool actionInProgress;

        protected bool QueryInProgress
        {
            get { return Thread.VolatileRead(ref this.queryDepth) > 0; }
        }

        private void BeginQuery()
        {
            Interlocked.Increment(ref this.queryDepth);
        }

        private void EndQuery()
        {
            if (Interlocked.Decrement(ref this.queryDepth) < 0)
            {
                Interlocked.Exchange(ref this.queryDepth, 0);
            }
        }

        protected bool ActionInProgress
        {
            get { return this.actionInProgress; }
        }

        private void MarkActionInProgress(bool running)
        {
            this.actionInProgress = running;
        }

        private void NotifyActionCommunicationFailure()
        {
            this.NotifyLoadFailure(null);
        }

        private void NotifyActionCommunicationSuccess()
        {
            this.SettleNoticeAfterServerContact(false);
        }

        protected virtual bool CanStartAction()
        {
            return !this.actionInProgress && !this.QueryInProgress;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (this.actionInProgress && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.ShowToast(
                        this.ActionRunningText,
                        Modern.Lab.Controls.Wpf.Display.ToastKind.Warning);
                return;
            }

            base.OnFormClosing(e);
        }

        protected virtual string ActionRunningText
        {
            get { return "Another action is still running."; }
        }

        private int busyDepth;

        private int busyGeneration;

        protected IDisposable Busy(string message)
        {
            return this.Busy(message, string.Empty, null);
        }

        protected IDisposable Busy(string message, string subMessage)
        {
            return this.Busy(message, subMessage, null);
        }

        protected IDisposable Busy(string message, string subMessage, Control region)
        {
            this.busyDepth = this.busyDepth + 1;
            this.ShowBusy(message, subMessage, region);

            return new BusyScope(this, this.busyGeneration);
        }

        protected void ResetBusy()
        {
            this.busyGeneration = this.busyGeneration + 1;
            this.busyDepth = 0;
            this.HideBusy();
        }

        private void ReleaseBusy(int generation)
        {
            if (generation != this.busyGeneration)
            {
                return;
            }

            this.busyDepth = this.busyDepth - 1;

            if (this.busyDepth <= 0)
            {
                this.busyDepth = 0;
                this.HideBusy();
            }
        }

        private sealed class BusyScope : IDisposable
        {
            private readonly int generation;
            private ModernFormBase owner;

            internal BusyScope(ModernFormBase owner, int generation)
            {
                this.owner = owner;
                this.generation = generation;
            }

            public void Dispose()
            {
                if (this.owner == null)
                {
                    return;
                }

                ModernFormBase target = this.owner;
                this.owner = null;
                target.ReleaseBusy(this.generation);
            }
        }

        private void ShowBusy(string message)
        {
            this.ShowBusy(message, string.Empty, null);
        }

        private void ShowBusy(string message, string subMessage)
        {
            this.ShowBusy(message, subMessage, null);
        }

        private void ShowBusy(string message, string subMessage, Control region)
        {
            if (this.IsDisposed || this.Disposing)
            {
                return;
            }

            Control host = region ?? (Control)this;

            if (this.sharedBusyOverlay == null)
            {
                this.sharedBusyOverlay = new Modern.Lab.WinForms.Controls.Display.ModernBusyOverlay();
            }

            if (!object.ReferenceEquals(this.sharedBusyOverlay.Parent, host))
            {
                if (this.sharedBusyOverlay.Parent != null)
                {
                    this.sharedBusyOverlay.Parent.Controls.Remove(this.sharedBusyOverlay);
                }

                host.Controls.Add(this.sharedBusyOverlay);
            }

            this.sharedBusyOverlay.Message = message;
            this.sharedBusyOverlay.SubMessage = subMessage ?? string.Empty;
            this.sharedBusyOverlay.BringToFront();
            this.sharedBusyOverlay.Busy = true;
        }

        private void HideBusy()
        {
            if (this.sharedBusyOverlay != null)
            {
                this.sharedBusyOverlay.Busy = false;
            }
        }

        protected virtual string ServerFailurePrefix
        {
            get { return "Server call failed: "; }
        }

        protected virtual string ServerFailureText(Exception failure)
        {
            return this.ServerFailurePrefix + (failure == null ? string.Empty : failure.Message);
        }





        protected virtual void ShowServerFailure(Exception failure)
        {
            if (this.LoadFailureSilent)
            {
                this.RenderStickyNotice();
                return;
            }

            this.RenderStickyNotice();
            this.pendingFailureTexts.Add(this.ServerFailureText(failure));
            this.Debounce(failureDialogKey, 150, new MethodInvoker(this.FlushFailureDialog));
        }

        private void FlushFailureDialog()
        {
            if (this.IsDisposed || this.Disposing || this.pendingFailureTexts.Count == 0)
            {
                return;
            }

            if (this.QueryInProgress)
            {
                this.Debounce(failureDialogKey, 150, new MethodInvoker(this.FlushFailureDialog));
                return;
            }

            string first = this.pendingFailureTexts[0];
            int count = this.pendingFailureTexts.Count;
            this.pendingFailureTexts.Clear();

            string message = count == 1
                    ? first
                    : count.ToString(System.Globalization.CultureInfo.InvariantCulture)
                            + " queries failed. " + first;

            this.ShowErrorMessage(
                    this.QueryFailedCaption, this.BuildStaleNoticeText(message));
        }





        protected bool LoadFailureSilent
        {
            get { return this.loadFailureSilent; }
        }

        protected virtual bool Confirm(string message, string caption)
        {
            return Modern.Lab.WinForms.Controls.Dialogs.ModernMessageDialog.Confirm(
                    this, caption, message);
        }






        internal static Action<Modern.Lab.Controls.Wpf.Display.ModernMessageKind, string, string>
                MessageInterceptor;

        protected virtual void ShowMessage(
                Modern.Lab.Controls.Wpf.Display.ModernMessageKind kind,
                string caption,
                string message)
        {
            Action<Modern.Lab.Controls.Wpf.Display.ModernMessageKind, string, string> interceptor =
                    MessageInterceptor;

            if (interceptor != null)
            {
                interceptor(kind, caption, message);
                return;
            }

            Modern.Lab.WinForms.Controls.Dialogs.ModernMessageDialog.Show(
                    this,
                    kind,
                    caption,
                    message,
                    MessageBoxButtons.OK);
        }

        protected void ShowErrorMessage(string caption, string message)
        {
            this.ShowMessage(
                    Modern.Lab.Controls.Wpf.Display.ModernMessageKind.Error, caption, message);
        }

        protected void Debounce(string key, int delayMs, MethodInvoker action)
        {
            if (action == null || this.IsDisposed || this.Disposing)
            {
                return;
            }

            string name = key ?? string.Empty;
            System.Windows.Forms.Timer timer;

            if (!this.debounceTimers.TryGetValue(name, out timer))
            {
                timer = new System.Windows.Forms.Timer();
                timer.Tick += this.OnDebounceTick;
                this.debounceTimers[name] = timer;
            }

            timer.Stop();
            timer.Interval = delayMs < 1 ? 1 : delayMs;

            timer.Tag = action;
            timer.Start();
        }

        protected void CancelDebounce(string key)
        {
            System.Windows.Forms.Timer timer;

            if (this.debounceTimers.TryGetValue(key ?? string.Empty, out timer))
            {
                timer.Stop();
            }
        }

        private void OnDebounceTick(object sender, EventArgs e)
        {
            System.Windows.Forms.Timer timer = sender as System.Windows.Forms.Timer;

            if (timer == null)
            {
                return;
            }

            timer.Stop();

            MethodInvoker action = timer.Tag as MethodInvoker;

            if (action != null && !this.IsDisposed && !this.Disposing)
            {
                action();
            }
        }

        protected virtual string ExcelDialogFilter
        {
            get { return "Excel Workbook|*.xlsx"; }
        }

        protected virtual string ExportEmptyText()
        {
            return "Nothing to export.";
        }

        protected virtual string ExportDoneText(int rowCount, string rowNoun)
        {
            return rowCount.ToString("N0") + " " + rowNoun + " exported.";
        }

        protected virtual string ExportFailedText(Exception failure)
        {
            return "Export failed: " + (failure == null ? string.Empty : failure.Message);
        }

        protected bool ExportToExcel(
            Modern.Lab.WinForms.Controls.Data.IExcelExportGrid grid,
            string sheetName,
            System.Data.DataTable data,
            string fileNamePrefix)
        {
            return this.ExportToExcel(grid, sheetName, data, fileNamePrefix, "rows");
        }

        protected bool ExportToExcel(
            Modern.Lab.WinForms.Controls.Data.IExcelExportGrid grid,
            string sheetName,
            System.Data.DataTable data,
            string fileNamePrefix,
            string rowNoun)
        {
            if (grid == null)
            {
                return false;
            }

            if (data == null || data.Rows.Count == 0)
            {
                this.ShowToast(
                    this.ExportEmptyText(),
                    Modern.Lab.Controls.Wpf.Display.ToastKind.Warning);
                return false;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = this.ExcelDialogFilter;
                dialog.FileName = (fileNamePrefix ?? "Export")
                        + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return false;
                }

                try
                {
                    grid.ExportXlsx(dialog.FileName, sheetName, data);
                    this.ShowToast(
                        this.ExportDoneText(data.Rows.Count, rowNoun ?? "rows"),
                        Modern.Lab.Controls.Wpf.Display.ToastKind.Success);
                    return true;
                }
                catch (Exception exception)
                {
                    this.ShowToast(
                        this.ExportFailedText(exception),
                        Modern.Lab.Controls.Wpf.Display.ToastKind.Error);
                    return false;
                }
            }
        }

        protected void PostToUi(MethodInvoker action)
        {
            this.TryPostToUi(action);
        }

        private bool TryPostToUi(MethodInvoker action)
        {
            if (action == null || this.IsDisposed || this.Disposing || !this.IsHandleCreated)
            {
                return false;
            }

            try
            {
                this.BeginInvoke(action);
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        protected void LoadAsync<T>(string channel, Func<T> fetch, Action<T> apply, MethodInvoker onComplete)
        {
            this.LoadAsync(channel, fetch, apply,
                    onComplete == null
                            ? (Action<bool>)null
                            : delegate(bool current) { onComplete(); });
        }

        protected void LoadAsync<T>(string channel, Func<T> fetch, Action<T> apply, Action<bool> onComplete)
        {
            if (fetch == null)
            {
                if (onComplete != null)
                {
                    onComplete(false);
                }

                return;
            }

            string key = channel ?? string.Empty;
            int version = this.NextLoadVersion(key);

            this.BeginQuery();

            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                T result = default(T);
                Exception failure = null;

                try
                {
                    result = fetch();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }

                bool posted = this.TryPostToUi(new MethodInvoker(delegate
                {
                    try
                    {
                        bool current = this.SettleLoad(channel, key, version, failure);

                        if (current && failure == null && apply != null)
                        {
                            apply(result);
                        }

                        if (onComplete != null)
                        {
                            onComplete(current);
                        }
                    }
                    finally
                    {
                        this.EndQuery();
                    }
                }));

                if (!posted)
                {
                    this.EndQuery();
                }
            });
        }

        protected void LoadAsync<T>(string channel, Func<T> fetch, Action<T> apply)
        {
            this.LoadAsync(channel, fetch, apply, (Action<bool>)null);
        }

        protected System.Threading.Tasks.Task<LoadOutcome<T>> FetchAsync<T>(
            string channel, Func<T> fetch)
        {
            return this.FetchAsync(channel, fetch, false);
        }





        protected System.Threading.Tasks.Task<LoadOutcome<T>> FetchAsync<T>(
            string channel, Func<T> fetch, bool silent)
        {
            string key = channel ?? string.Empty;

            System.Threading.Tasks.TaskCompletionSource<LoadOutcome<T>> completion =
                    new System.Threading.Tasks.TaskCompletionSource<LoadOutcome<T>>();

            if (fetch == null)
            {
                completion.SetResult(new LoadOutcome<T>(false, default(T), null));
                return completion.Task;
            }

            int version = this.NextLoadVersion(key);

            this.BeginQuery();

            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                T result = default(T);
                Exception failure = null;

                try
                {
                    result = fetch();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }

                bool posted = this.TryPostToUi(new MethodInvoker(delegate
                {
                    LoadOutcome<T> outcome;

                    try
                    {
                        bool current = this.SettleLoad(channel, key, version, failure, silent);
                        outcome = new LoadOutcome<T>(current, result, failure);
                    }
                    catch (Exception settleFailure)
                    {
                        outcome = new LoadOutcome<T>(false, result, failure ?? settleFailure);
                    }

                    this.EndQuery();
                    completion.TrySetResult(outcome);
                }));

                if (!posted)
                {
                    this.EndQuery();
                    completion.TrySetResult(new LoadOutcome<T>(false, result, failure));
                }
            });

            return completion.Task;
        }

        protected void InvalidateChannel(string channel)
        {
            string key = channel ?? string.Empty;
            this.NextLoadVersion(key);

            this.staleChannels.Remove(key);
            this.silentFailureChannels.Remove(key);
            this.RenderStickyNotice();
        }

        protected MethodInvoker WhenAll(int count, MethodInvoker onAll)
        {
            if (count < 1)
            {
                if (onAll != null)
                {
                    onAll();
                }

                return new MethodInvoker(delegate { });
            }

            int[] remaining = new int[] { count };

            return new MethodInvoker(delegate
            {
                if (remaining[0] <= 0)
                {
                    return;
                }

                remaining[0]--;

                if (remaining[0] == 0 && onAll != null)
                {
                    onAll();
                }
            });
        }

        protected virtual void OnLoadFailed(string channel, Exception failure)
        {
            this.ShowServerFailure(failure);
        }

        private bool SettleLoad(string channel, string key, int version, Exception failure)
        {
            return this.SettleLoad(channel, key, version, failure, false);
        }

        private bool SettleLoad(
                string channel, string key, int version, Exception failure, bool silent)
        {
            bool current = !this.IsDisposed
                    && !this.Disposing
                    && this.IsCurrentLoadVersion(key, version);

            if (current)
            {
                if (failure != null)
                {
                    this.NotifyLoadFailure(key, silent);
                    this.loadFailureSilent = silent;

                    try
                    {
                        this.OnLoadFailed(channel, failure);
                    }
                    finally
                    {
                        this.loadFailureSilent = false;
                    }
                }
                else
                {
                    this.NotifyLoadSuccess(key);
                }
            }

            return current;
        }

        private int NextLoadVersion(string channel)
        {
            int version;
            this.loadVersions.TryGetValue(channel, out version);
            version++;
            this.loadVersions[channel] = version;
            return version;
        }

        private bool IsCurrentLoadVersion(string channel, int version)
        {
            int current;
            return this.loadVersions.TryGetValue(channel, out current) && current == version;
        }

        protected bool IsServerConnectionLost
        {
            get { return this.connectionLost; }
        }

        protected virtual void OnServerConnectionChanged(bool lost)
        {
        }

        private void NotifyLoadFailure(string channel, bool silent = false)
        {
            if (this.IsDisposed || this.Disposing)
            {
                return;
            }

            if (channel != null)
            {
                this.staleChannels.Add(channel);
                if (silent)
                {
                    this.silentFailureChannels.Add(channel);
                }
                else
                {
                    this.silentFailureChannels.Remove(channel);
                }
            }

            bool wasLost = this.connectionLost;
            this.connectionLost = true;
            this.failureNoticeShowing = true;

            if (!wasLost)
            {
                this.OnServerConnectionChanged(true);
            }
        }

        private string BuildStaleNoticeText(string lead)
        {
            return this.BuildStaleNoticeText(lead, this.staleChannels);
        }

        private string BuildStaleNoticeText(string lead, ICollection<string> channels)
        {
            DateTime basis;

            if (channels.Count > 0)
            {
                basis = DateTime.MaxValue;

                foreach (string staleChannel in channels)
                {
                    DateTime channelTime;

                    if (!this.lastLoadSuccessTimes.TryGetValue(staleChannel, out channelTime))
                    {
                        basis = DateTime.MinValue;
                        break;
                    }

                    if (channelTime < basis)
                    {
                        basis = channelTime;
                    }
                }
            }
            else
            {
                basis = this.lastAnyLoadSuccessTime;
            }

            return basis == DateTime.MinValue || basis == DateTime.MaxValue
                    ? lead + " — no data received yet."
                    : lead + " — showing data as of "
                            + basis.ToString(
                                    "HH:mm:ss",
                                    System.Globalization.CultureInfo.InvariantCulture)
                            + ".";
        }

        private void NotifyLoadSuccess(string channel)
        {
            DateTime now = DateTime.Now;
            this.lastLoadSuccessTimes[channel] = now;
            this.lastAnyLoadSuccessTime = now;
            this.staleChannels.Remove(channel);
            this.silentFailureChannels.Remove(channel);
            this.SettleNoticeAfterServerContact(true);
        }

        private void SettleNoticeAfterServerContact(bool refreshedByData)
        {
            if (this.IsDisposed || this.Disposing)
            {
                return;
            }

            bool wasLost = this.connectionLost;
            this.connectionLost = false;

            if (this.failureNoticeShowing)
            {
                if (this.staleChannels.Count == 0)
                {
                    this.failureNoticeShowing = false;
                    this.renderedNoticeBasis = string.Empty;

                    if (this.contractNoticeText.Length == 0)
                    {
                        this.ShowTransientNotice(
                                refreshedByData
                                        ? "Reconnected — data refreshed."
                                        : "Reconnected — server responded.",
                                Modern.Lab.Controls.Wpf.Display.ToastKind.Success);
                        this.renderedNoticeBasis = this.NoticeBasis();
                        return;
                    }

                    this.RenderStickyNotice();
                }
                else
                {
                    this.RenderStickyNotice();
                }
            }

            if (wasLost)
            {
                this.OnServerConnectionChanged(false);
            }
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.DisposeMessaging();

                foreach (KeyValuePair<string, System.Windows.Forms.Timer> entry in this.debounceTimers)
                {
                    entry.Value.Stop();
                    entry.Value.Dispose();
                }

                this.debounceTimers.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
