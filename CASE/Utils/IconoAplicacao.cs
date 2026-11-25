using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System;

namespace CASE.Utils
{
    internal static class IconoAplicacao
    {
        private static Icon iconePrincipal;
        public static void Aplicar(Form formulario)
        {
            if (formulario == null)
            {
                throw new ArgumentNullException(nameof(formulario));
            }

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                return;
            }

            if (iconePrincipal == null)
            {
                using (var recurso = typeof(IconoAplicacao).Assembly.GetManifestResourceStream("CASE.Resources.CASE.ico"))
                {
                    if (recurso == null)
                        iconePrincipal = (Icon)SystemIcons.Application.Clone();
                    else
                        using (var icone = new Icon(recurso))
                            iconePrincipal = (Icon)icone.Clone();
                }
            }

            formulario.Icon = (Icon)iconePrincipal.Clone();
        }
    }
}
