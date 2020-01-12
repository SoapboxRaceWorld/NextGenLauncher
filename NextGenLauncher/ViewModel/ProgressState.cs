using GalaSoft.MvvmLight;

namespace NextGenLauncher.ViewModel
{
    public class ProgressState : ObservableObject
    {
        private float _progressValue;
        private string _progressText;
        private bool _isIndeterminate;

        public float ProgressValue
        {
            get => _progressValue;
            set => Set(ref _progressValue, value);
        }

        public string ProgressText
        {
            get => _progressText;
            set => Set(ref _progressText, value);
        }

        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set => Set(ref _isIndeterminate, value);
        }
    }
}