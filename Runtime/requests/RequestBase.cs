using System;
using BeatThat.Properties;
using UnityEngine;


namespace BeatThat.Requests
{
    /// Convenience base impl for Request interface
    public abstract class RequestBase : Request, HasUploadProgress
    {
        public event Action StatusUpdated;

        protected RequestBase()
        {
            this.debug = RequestConfig.debugging;
        }

        public bool isCancelled { get { return this.status == RequestStatus.CANCELLED; } }

        public RequestStatus status { get; private set; }

        public bool hasError { get { return !string.IsNullOrEmpty(this.error); } }

        public string error { get; protected set; }

        public bool debug { get; set; }

        virtual public string loggingName { get { return GetType().Name; } }

        /// <summary>
        /// By default service request always report 0 as progress until done
        /// </summary>
        /// <value>The progress.</value>
        virtual public float progress { get { return 0f; } }

        /// <summary>
        /// By default service request always report 0 as uploadProgress until done
        /// </summary>
        /// <value>The progress.</value>
        virtual public float uploadProgress { get { return 0f; } }

        public string id { get; set; }

        protected void CompleteRequest(RequestStatus s = RequestStatus.DONE)
        {
            if (this.debug)
            {
                Debug.Log("[" + Time.frameCount + "] " + this.loggingName + "::CompleteRequest to status " + s + " (from status " + this.status + ")");
            }
            if (!this.allowCompleteFromCompletedStatus)
            {
                if (this.status == RequestStatus.DONE)
                {
                    Debug.Log("[" + Time.frameCount + "]" + this.loggingName  + "::Already done!");
                    return;
                }
                if (this.status == RequestStatus.CANCELLED)
                {
                    Debug.Log("[" + Time.frameCount + "]" + this.loggingName + "::Already cancelled!");
                    return;
                }
            }
            UpdateStatus(s, PropertyEventOptions.Disable);
            BeforeCompletionCallback();
            if (this.hasError)
            {
                Exec(this.onError);
            }
            else if (this.status == RequestStatus.CANCELLED)
            {
                Exec(this.onCancel);
            }
            else
            {
                Exec(this.onSuccess);
            }
            if (s != RequestStatus.CANCELLED || this.isCallbackOnCancelledEnabled)
            {
                Exec(this.callback);
            }
            ClearCallback();
            UpdateStatus(s, PropertyEventOptions.Force);
            AfterCompletionCallback();
        }

        protected void UpdateToQueued()
        {
            UpdateStatus(RequestStatus.QUEUED);
        }

        protected void UpdateToInProgress()
        {
            UpdateStatus(RequestStatus.IN_PROGRESS);
        }

        protected void UpdateToCancelled()
        {
            UpdateStatus(RequestStatus.CANCELLED);
        }

        protected void CompleteWithError(string err)
        {
            this.error = err;
            CompleteRequest();
        }

        /// <summary>
        /// For most requests, it is an error to call CompleteRequest when the status is already DONE or CANCELLED,
        /// but some request types (like a local error) get marked status DONE as soon as they're created...
        /// </summary>
        protected bool allowCompleteFromCompletedStatus { get; set; }

        virtual protected void BeforeCompletionCallback() { }
        virtual protected void AfterCompletionCallback() { }

        protected void UpdateStatus(RequestStatus s, PropertyEventOptions opts = PropertyEventOptions.SendOnChange)
        {
            if (this.debug)
            {
                Debug.Log("[" + Time.frameCount + "] " + GetType() + "::UpdateStatus to status " + s + " (from status " + this.status + ")  anyListeners=" + (this.StatusUpdated != null));
            }
            if (this.status == s && opts != PropertyEventOptions.Force)
            {
                return;
            }
            this.status = s;
            if (opts == PropertyEventOptions.Disable)
            {
                return;
            }
            Exec(this.StatusUpdated);
        }

        public void Dispose()
        {
            if (this.debug)
            {
                Debug.Log("[" + Time.frameCount + "] " + GetType() + "::Dispose");
            }

            if (this.IsQueuedOrInProgress())
            {
                Cancel();
            }

            DisposeRequest();
        }

        virtual protected void DisposeRequest()
        {
        }

        public void Cancel()
        {
            if (this.debug)
            {
                Debug.Log("[" + Time.frameCount + "] " + GetType() + "::Cancel");
            }
            if (!this.IsQueuedOrInProgress())
            {
                return;
            }
            BeforeCancel();
            CompleteRequest(RequestStatus.CANCELLED);
            AfterCancel();
            DisposeRequest();
        }

        virtual protected void BeforeCancel() { }
        virtual protected void AfterCancel() { }

        public bool isCallbackOnCancelledEnabled { get; set; }

        public void Execute(Action callback = null, bool callbackOnCancelled = false)
        {
            if (this.debug)
            {
                Debug.Log("[" + Time.frameCount + "] " + GetType() + "::Execute");
            }

            this.isCallbackOnCancelledEnabled = callbackOnCancelled;
            this.callback = callback;
            ExecuteRequest();

            if (this.status == RequestStatus.NONE)
            {
                UpdateToQueued();
            }
        }

        public void Send(Action onSuccess = null, Action onError = null, Action onCancel = null)
        {
            if (this.debug)
            {
                Debug.Log("[" + Time.frameCount + "] " + GetType() + "::Send");
            }

            this.onSuccess = onSuccess;
            this.onError = onError;
            this.onCancel = onCancel;

            ExecuteRequest();

            if (this.status == RequestStatus.NONE)
            {
                UpdateToQueued();
            }
        }


        protected void ClearCallback()
        {
            this.callback = null;
            this.onSuccess = null;
            this.onError = null;
            this.onCancel = null;
        }

        protected Action callback { get; private set; }

        protected Action onSuccess { get; private set; }
        protected Action onError { get; private set; }
        protected Action onCancel { get; private set; }

        abstract protected void ExecuteRequest();

        private static void Exec(Action a)
        {
            if (a != null)
            {
                a();
            }
        }

    }

}

