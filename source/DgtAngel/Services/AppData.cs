using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DgtAngel.Services
{
    public interface IAppData
    {
        int Age { get; set; }

        event Action OnChange;
    }

    public class AppData : IAppData
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



