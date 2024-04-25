namespace SuperEncode.Wpf.ViewModels
{
    public class VideoSetting : BaseViewModel
    {
        private bool _enableHdr;

        public bool EnableHdr
        {
            get => _enableHdr;
            set => SetField(ref _enableHdr, value);
        }

        private bool _enableCmd;

        public bool EnableCmd
        {
            get => _enableCmd;
            set => SetField(ref _enableCmd, value);
        }
        

        private bool _deleteAfterEncode;

        public bool DeleteAfterEncode
        {
            get => _deleteAfterEncode;
            set => SetField(ref _deleteAfterEncode, value);
        }

        private string _inputFolder = string.Empty;

        public string InputFolder
        {
            get => _inputFolder;
            set => SetField(ref _inputFolder, value);
        }

    }
}
