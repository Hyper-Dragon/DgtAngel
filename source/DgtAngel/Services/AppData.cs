using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DgtAngel.Services
{
    public class AppData
    {
        public int Age { get; set; }

        private int _number;
        public int Number
        {
            get
            {
                return _number;
            }
            set
            {
                _number = value;
                NotifyDataChanged();
            }
        }

        private string _color;
        public string Color
        {
            get
            {
                return _color;
            }
            set
            {
                _color = value;
                NotifyDataChanged();
            }
        }

        public event Action OnChange;

        private void NotifyDataChanged() => OnChange?.Invoke();
    }



}



