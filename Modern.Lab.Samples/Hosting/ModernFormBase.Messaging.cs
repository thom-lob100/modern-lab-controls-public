using System;
using System.Collections.Generic;
using System.Data;

using Modern.Lab.Hosting.Messaging;

namespace Modern.Lab.Hosting
{


    public partial class ModernFormBase
    {

        protected virtual bool UseServerMessaging
        {
            get { return false; }
        }


        protected virtual string SendRequestText(string request)
        {

            throw new NotSupportedException(
                    "Server messaging is not wired yet — see ModernFormBase.SendRequestText.");
        }

        protected virtual DataTable ParseQueryData(string sendMessage)
        {
            return ServerMessageFormat.ParseQueryTable(sendMessage);
        }


        protected DataActionResult Request(string requestText)
        {
            DataActionResult reply;

            try
            {
                reply = this.Exchange(requestText);
            }
            catch (Exception failure)
            {
                reply = DataActionResult.CommunicationLost(this.ServerFailureText(failure));
            }

            reply.SetTableParser(this.ParseQueryData);

            if (!reply.Success && !reply.CommunicationFailure && !this.IsNoDataReply(reply))
            {
                reply.MarkQueryFault();
            }

            return reply;
        }





        protected virtual bool IsNoDataReply(DataActionResult reply)
        {
            if (reply == null)
            {
                return false;
            }

            return string.Equals(reply.Code, "NOT_FOUND", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(reply.Code, "DATA_NOT_FOUND", StringComparison.OrdinalIgnoreCase);
        }

        private DataActionResult Exchange(string requestText)
        {
            return DataActionResult.FromReply(this.SendRequestText(requestText));
        }


        protected void RunAction(Func<DataActionResult> call, Action<DataActionResult> onSuccess)
        {
            this.RunAction(call, onSuccess, null, this.ActionBusyText);
        }

        protected void RunAction(
                Func<DataActionResult> call, Action<DataActionResult> onSuccess, string busyText)
        {
            this.RunAction(call, onSuccess, null, busyText);
        }

        protected void RunAction(
                Func<DataActionResult> call,
                Action<DataActionResult> onSuccess,
                Action<DataActionResult> onRejected,
                string busyText)
        {
            if (call == null)
            {
                return;
            }

            this.RunActionCoreAsync(call, onSuccess, onRejected, busyText);
        }

        protected void RunActionBatch<T>(
                IList<T> items,
                Func<T, DataActionResult> call,
                Action<BatchProgress> onProgress,
                Action<BatchOutcome> onDone,
                string busyText)
        {
            if (call == null || items == null || items.Count == 0)
            {
                return;
            }

            this.RunActionBatchCoreAsync(items, call, onProgress, onDone, busyText);
        }

        protected virtual string ActionBusyText
        {
            get { return "Working…"; }
        }

        private async void RunActionCoreAsync(
                Func<DataActionResult> call,
                Action<DataActionResult> onSuccess,
                Action<DataActionResult> onRejected,
                string busyText)
        {
            if (!this.BeginAction())
            {
                return;
            }

            DataActionResult reply = null;
            Exception callerMistake = null;

            try
            {
                using (this.Busy(busyText ?? this.ActionBusyText))
                {
                    reply = await System.Threading.Tasks.Task.Run(call);
                }
            }
            catch (Exception failure)
            {
                reply = this.ClassifyActionFailure(failure, out callerMistake);
            }
            finally
            {
                this.EndAction();
            }

            if (this.IsDisposed || this.Disposing)
            {
                return;
            }

            if (callerMistake != null)
            {
                this.ShowCallerMistake(callerMistake);
                return;
            }

            try
            {
                this.SettleAction(reply, onSuccess, onRejected);
            }
            catch (Exception callbackFailure)
            {
                this.ShowServerFailure(callbackFailure);
            }
        }

        private async void RunActionBatchCoreAsync<T>(
                IList<T> items,
                Func<T, DataActionResult> call,
                Action<BatchProgress> onProgress,
                Action<BatchOutcome> onDone,
                string busyText)
        {
            if (!this.BeginAction())
            {
                return;
            }

            int total = items.Count;
            int succeeded = 0;
            int failed = 0;
            DataActionResult firstFailure = null;
            Exception callerMistake = null;

            bool communicationFailureSeen = false;

            try
            {
                using (this.Busy(busyText ?? this.ActionBusyText))
                {
                    for (int index = 0; index < total; index = index + 1)
                    {
                        T item = items[index];
                        DataActionResult one;

                        try
                        {
                            one = await System.Threading.Tasks.Task.Run(
                                    delegate { return call(item); });
                        }
                        catch (Exception failure)
                        {
                            one = this.ClassifyActionFailure(failure, out callerMistake);
                        }

                        if (callerMistake != null)
                        {
                            break;
                        }

                        if (one.Success)
                        {
                            succeeded = succeeded + 1;
                        }
                        else
                        {
                            failed = failed + 1;

                            if (firstFailure == null)
                            {
                                firstFailure = one;
                            }

                            if (one.CommunicationFailure)
                            {
                                communicationFailureSeen = true;
                            }
                        }

                        if (this.IsDisposed || this.Disposing)
                        {
                            return;
                        }

                        if (onProgress != null)
                        {
                            try
                            {
                                onProgress(new BatchProgress(index + 1, total, one));
                            }
                            catch (Exception progressFailure)
                            {
                                this.ShowServerFailure(progressFailure);
                            }
                        }
                    }
                }
            }
            finally
            {
                this.EndAction();
            }

            if (this.IsDisposed || this.Disposing)
            {
                return;
            }

            if (callerMistake != null)
            {
                this.ShowCallerMistake(callerMistake);
                return;
            }

            BatchOutcome outcome = new BatchOutcome(total, succeeded, failed, firstFailure);

            if (communicationFailureSeen)
            {
                this.NotifyActionCommunicationFailure();
            }
            else
            {
                this.NotifyActionCommunicationSuccess();
            }

            try
            {
                if (onDone != null)
                {
                    onDone(outcome);
                }
                else if (failed > 0 && firstFailure != null)
                {
                    this.ShowActionFailure(firstFailure);
                }
            }
            catch (Exception callbackFailure)
            {
                this.ShowServerFailure(callbackFailure);
            }
        }

        private DataActionResult ClassifyActionFailure(
                Exception failure, out Exception callerMistake)
        {
            ArgumentException contractFailure = failure as ArgumentException;

            if (contractFailure != null)
            {
                callerMistake = contractFailure;
                return null;
            }

            callerMistake = null;

            return DataActionResult.CommunicationLost(this.ServerFailureText(failure));
        }

        private bool BeginAction()
        {
            if (this.IsDisposed || this.Disposing)
            {
                return false;
            }

            if (!this.CanStartAction())
            {
                this.ShowToast(
                        this.ActionRunningText,
                        Modern.Lab.Controls.Wpf.Display.ToastKind.Warning);
                return false;
            }

            this.MarkActionInProgress(true);
            return true;
        }

        private void EndAction()
        {
            this.MarkActionInProgress(false);
        }

        private void SettleAction(
                DataActionResult reply,
                Action<DataActionResult> onSuccess,
                Action<DataActionResult> onRejected)
        {
            if (reply.CommunicationFailure)
            {
                this.NotifyActionCommunicationFailure();
            }
            else
            {
                this.NotifyActionCommunicationSuccess();
            }

            if (reply.Success)
            {
                if (onSuccess != null)
                {
                    onSuccess(reply);
                }

                return;
            }

            if (onRejected != null)
            {
                onRejected(reply);
                return;
            }

            this.ShowActionFailure(reply);
        }

        protected virtual void ShowActionFailure(DataActionResult reply)
        {
            if (reply == null)
            {
                return;
            }

            string reason = string.IsNullOrEmpty(reply.Message)
                    ? this.ActionFailedText
                    : reply.Message;

            this.ShowMessage(
                    Modern.Lab.Controls.Wpf.Display.ModernMessageKind.Error,
                    this.ActionFailedText,
                    reason);
        }

        protected virtual void ShowCallerMistake(Exception mistake)
        {
            if (mistake == null)
            {
                return;
            }

            this.ShowMessage(
                    Modern.Lab.Controls.Wpf.Display.ModernMessageKind.Error,
                    this.CallerMistakeText,
                    mistake.Message + Environment.NewLine + Environment.NewLine
                            + mistake.GetType().FullName);
        }

        protected virtual string CallerMistakeText
        {
            get { return "The request was built incorrectly (developer error)."; }
        }

        protected virtual string ActionFailedText
        {
            get { return "The request could not be completed."; }
        }

        public sealed class BatchProgress
        {
            internal BatchProgress(int completed, int total, DataActionResult last)
            {
                this.Completed = completed;
                this.Total = total;
                this.Last = last;
            }

            public int Completed { get; private set; }

            public int Total { get; private set; }

            public DataActionResult Last { get; private set; }

            public string Text
            {
                get
                {
                    return this.Completed.ToString(
                                    System.Globalization.CultureInfo.InvariantCulture)
                            + " / "
                            + this.Total.ToString(
                                    System.Globalization.CultureInfo.InvariantCulture);
                }
            }
        }

        public sealed class BatchOutcome
        {
            internal BatchOutcome(
                    int total, int succeeded, int failed, DataActionResult firstFailure)
            {
                this.Total = total;
                this.Succeeded = succeeded;
                this.Failed = failed;
                this.FirstFailure = firstFailure;
            }

            public int Total { get; private set; }

            public int Succeeded { get; private set; }

            public int Failed { get; private set; }

            public DataActionResult FirstFailure { get; private set; }

            public bool AllSucceeded
            {
                get { return this.Failed == 0; }
            }

            public string Text
            {
                get
                {
                    System.Globalization.CultureInfo culture =
                            System.Globalization.CultureInfo.InvariantCulture;

                    if (this.Failed == 0)
                    {
                        return this.Succeeded.ToString(culture) + " succeeded";
                    }

                    return this.Succeeded.ToString(culture) + " succeeded, "
                            + this.Failed.ToString(culture) + " failed";
                }
            }
        }
    }
}
