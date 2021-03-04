using System;
using System.Windows.Forms;
using Gecko;
using xNet;

namespace Coinbase_Monitoring_New
{
    public partial class Form2 : Form
    {
        public string AccessToken;
        public Form2()
        {
            InitializeComponent();
            Xpcom.Initialize("Firefox");
            geckoWebBrowser1.Navigate("https://www.coinbase.com/oauth/authorize?client_id=3843dcdb8a84798dbf01aab35cf04ca702db111da7e1c65a89c2210122aff2b0&redirect_uri=https%3A%2F%2Fgoogle.com%2F1&response_type=code&scope=wallet:accounts:read");
        }

        private void geckoWebBrowser1_DocumentCompleted(object sender, Gecko.Events.GeckoDocumentCompletedEventArgs e)
        {
            if (this.geckoWebBrowser1.Url.ToString().Contains("?code="))
            {
                AccessToken = this.geckoWebBrowser1.Url.ToString().Substring("?code=");
                this.Close();
            }
        }
    }
}
