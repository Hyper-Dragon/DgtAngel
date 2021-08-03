using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DgtAngel.Services
{
    public class AppData
    {


        private int _age;
        public int Age
        {
            get
            {
                return _age;
            }
            set
            {
                _age = value;
                NotifyDataChanged();
            }
        }


        public event Action OnChange;

        private void NotifyDataChanged() => OnChange?.Invoke();
    }



}



