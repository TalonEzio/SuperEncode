﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace SuperEncode.Wpf.ViewModels
{
    public partial class VideoPlayerViewModel : ObservableObject
    {
        [ObservableProperty]
        private long currentTime;
        [ObservableProperty]
        private long totalTime;
    }
}
