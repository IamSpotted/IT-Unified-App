using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MauiApp1.Models
{
    public class NetOpRestartResult : INotifyPropertyChanged
    {
        private string _computerName = string.Empty;
        private string _status = "Pending";
        private string _message = string.Empty;
        private string _details = string.Empty;
        private Color _statusColor = Colors.Orange;
        private bool _isCompleted = false;
        private DateTime _timestamp = DateTime.Now;

        public string ComputerName
        {
            get => _computerName;
            set
            {
                _computerName = value;
                OnPropertyChanged();
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                UpdateStatusColor();
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        public string Details
        {
            get => _details;
            set
            {
                _details = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasDetails));
            }
        }

        public Color StatusColor
        {
            get => _statusColor;
            private set
            {
                _statusColor = value;
                OnPropertyChanged();
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                _isCompleted = value;
                OnPropertyChanged();
                if (value)
                {
                    Timestamp = DateTime.Now;
                }
            }
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set
            {
                _timestamp = value;
                OnPropertyChanged();
            }
        }

        public bool HasDetails => !string.IsNullOrEmpty(Details);

        private void UpdateStatusColor()
        {
            StatusColor = Status.ToLower() switch
            {
                "success" or "completed" => Colors.Green,
                "failed" or "error" => Colors.Red,
                "processing" or "restarting" => Colors.Blue,
                "warning" => Colors.Orange,
                _ => Colors.Gray
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
