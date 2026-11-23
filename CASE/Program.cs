using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
namespace CASE
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            foreach (string arquivo in new[] { "MySql.Data.dll", "BouncyCastle.Cryptography.dll", "CASE.exe.config" })
            {
                if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, arquivo)))
                {
                    MessageBox.Show(
                        "A pasta do CASE está incompleta. Extraia todos os arquivos do pacote e abra o CASE.exe dentro da pasta extraída. Não mova apenas o executável.",
                        "Arquivos do CASE incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            Application.Run(new LoginForm());
        }
    }
}
