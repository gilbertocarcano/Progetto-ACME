using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace AcmeUI.Models
{
    public class ScanRow : ObservableObject
    {
        private float _position;
        public float Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        private float _b1;
        public float B1
        {
            get => _b1;
            set => SetProperty(ref _b1, value);
        }

        private float _b2;
        public float B2
        {
            get => _b2;
            set => SetProperty(ref _b2, value);
        }

        private float _b3;
        public float B3
        {
            get => _b3;
            set => SetProperty(ref _b3, value);
        }

        private float _b4;
        public float B4
        {
            get => _b4;
            set => SetProperty(ref _b4, value);
        }

        private float _b5;
        public float B5
        {
            get => _b5;
            set => SetProperty(ref _b5, value);
        }

        private bool _isInHRRange;
        public bool IsInHRRange
        {
            get => _isInHRRange;
            set => SetProperty(ref _isInHRRange, value);
        }
    }

}
