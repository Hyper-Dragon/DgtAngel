using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DgtCherub
{
    public partial class Form1 : Form
    {
        private readonly ILogger _logger;
        //private readonly IAppDataService _appData;

        public Form1(ILogger<Form1> logger)//, IAppDataService appData)
        {
            _logger = logger;
            //_appData = appData;
            InitializeComponent();
        }

    }
}
