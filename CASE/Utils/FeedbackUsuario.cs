using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace CASE.Utils
{
    public enum TipoFeedback
    {
        Informacao,
        Sucesso,
        Atencao,
        Erro
    }

    /// <summary>
    /// Exibe mensagens curtas em um aviso temporário no topo da janela ativa,
    /// sem roubar o foco do usuário. Mensagens de erro técnico são convertidas
    /// em orientações compreensíveis.
    /// </summary>
    public static class FeedbackUsuario
    {
        private static readonly ConditionalWeakTable<Form, AvisoFlutuante> Avisos =
            new ConditionalWeakTable<Form, AvisoFlutuante>();

        public static void Exibir(Control contexto, string mensagem, TipoFeedback tipo = TipoFeedback.Informacao)
        {
            if (string.IsNullOrWhiteSpace(mensagem))
                return;

            Form janela = contexto as Form ?? contexto?.FindForm();
            if (janela == null || janela.IsDisposed || !janela.IsHandleCreated)
            {
                MessageBox.Show(contexto, mensagem, "CASE", MessageBoxButtons.OK,
                    tipo == TipoFeedback.Erro || tipo == TipoFeedback.Atencao
                        ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                return;
            }

            if (janela.InvokeRequired)
            {
                janela.BeginInvoke(new Action(() => Exibir(janela, mensagem, tipo)));
                return;
            }

            // Avisos flutuantes ficam restritos às telas principais. Em diálogos
            // modais, a mensagem local do formulário evita cobrir o conteúdo.
            if (EhDialogo(janela))
                return;

            AvisoFlutuante aviso;
            if (!Avisos.TryGetValue(janela, out aviso) || aviso.IsDisposed)
            {
                Avisos.Remove(janela);
                aviso = new AvisoFlutuante(janela);
                Avisos.Add(janela, aviso);
            }
            aviso.Mostrar(mensagem, tipo);
        }

        public static void ExibirErro(Control contexto, Exception erro, string mensagemPadrao,
            Label mensagemInline = null)
        {
            string mensagem = MensagemErro(erro, mensagemPadrao);
            Form janela = contexto as Form ?? contexto?.FindForm();
            if (mensagemInline != null && !mensagemInline.IsDisposed)
            {
                mensagemInline.Text = mensagem;
                mensagemInline.Visible = true;
                mensagemInline.ForeColor = Color.Firebrick;
            }

            if (janela != null && EhDialogo(janela))
            {
                if (mensagemInline == null)
                    MessageBox.Show(janela, mensagem, "CASE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Exibir(contexto, mensagem, TipoFeedback.Erro);
        }

        private static bool EhDialogo(Form janela)
        {
            if (janela == null)
                return false;

            return janela.Modal || janela.FormBorderStyle == FormBorderStyle.FixedDialog ||
                janela.FormBorderStyle == FormBorderStyle.FixedSingle ||
                janela.FormBorderStyle == FormBorderStyle.FixedToolWindow;
        }

        public static string MensagemErro(Exception erro, string mensagemPadrao)
        {
            string padrao = string.IsNullOrWhiteSpace(mensagemPadrao)
                ? "Não foi possível concluir a operação. Confira os dados e tente novamente."
                : mensagemPadrao;

            MySqlException erroBanco = erro as MySqlException;
            if (erroBanco != null)
            {
                switch (erroBanco.Number)
                {
                    case 1062:
                        return "Já existe um registro com essas informações. Confira os dados e tente novamente.";
                    case 1451:
                        return "Este registro está vinculado a outros dados e não pode ser removido.";
                    case 1452:
                        return "Selecione uma opção válida antes de salvar.";
                    case 1205:
                    case 1213:
                        return "A operação encontrou uma alteração simultânea. Atualize a tela e tente novamente.";
                    case 1042:
                    case 1045:
                    case 1049:
                    case 2002:
                    case 2003:
                        return "Não foi possível acessar o sistema agora. Tente novamente em alguns instantes.";
                    default:
                        return padrao;
                }
            }

            string mensagemSegura = erro?.Message?.Trim();
            if (string.IsNullOrWhiteSpace(mensagemSegura) || ContemDetalheTecnico(mensagemSegura))
                return padrao;

            if ((erro is ArgumentException || erro is InvalidOperationException ||
                erro is UnauthorizedAccessException) && !ContemDetalheTecnico(mensagemSegura))
                return mensagemSegura;

            return padrao;
        }

        private static bool ContemDetalheTecnico(string mensagem)
        {
            string[] termos =
            {
                "mysql", "sqlstate", "sql error", "constraint", "foreign key", "duplicate entry",
                "unknown column", "unknown table", "connection string", "stack trace", " at line ",
                "inner exception", "timeout expired", "data source", "access denied for user",
                "object reference", "nullreferenceexception", "invalid column", "index was outside",
                "cannot convert", "input string", "specified cast", "parameter is not",
                "disposed object", "collection was modified", "sequence contains", "at system.",
                "connection", "not open", "unable to", "failed to", "parameter name:",
                "value cannot be null", "was out of range", "file not found", "path is invalid"
            };
            foreach (string termo in termos)
                if (mensagem.IndexOf(termo, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        private sealed class AvisoFlutuante : Form
        {
            private readonly Form proprietaria;
            private readonly Panel faixa = new Panel();
            private readonly Label texto = new Label();
            private readonly Timer temporizador = new Timer();

            public AvisoFlutuante(Form proprietaria)
            {
                this.proprietaria = proprietaria;
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                ShowIcon = false;
                StartPosition = FormStartPosition.Manual;
                Size = new Size(520, 76);
                BackColor = Color.White;
                Padding = Padding.Empty;
                Cursor = Cursors.Hand;

                faixa.Dock = DockStyle.Left;
                faixa.Width = 7;
                Controls.Add(faixa);
                texto.Dock = DockStyle.Fill;
                texto.Padding = new Padding(18, 10, 18, 10);
                texto.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
                texto.ForeColor = Color.FromArgb(18, 47, 80);
                texto.TextAlign = ContentAlignment.MiddleLeft;
                texto.AutoEllipsis = true;
                texto.Cursor = Cursors.Hand;
                Controls.Add(texto);
                texto.BringToFront();

                temporizador.Interval = 8000;
                temporizador.Tick += (s, e) => Close();
                SizeChanged += (s, e) => VisualUtils.Arredondar(this, 10);
                Click += (s, e) => Close();
                texto.Click += (s, e) => Close();
                faixa.Click += (s, e) => Close();
                proprietaria.LocationChanged += ProprietariaAlterada;
                proprietaria.SizeChanged += ProprietariaAlterada;
                FormClosed += (s, e) =>
                {
                    temporizador.Stop();
                    temporizador.Dispose();
                    proprietaria.LocationChanged -= ProprietariaAlterada;
                    proprietaria.SizeChanged -= ProprietariaAlterada;
                };
            }

            protected override bool ShowWithoutActivation => true;

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams parametros = base.CreateParams;
                    parametros.ExStyle |= 0x08000000 | 0x00000080; // NOACTIVATE | TOOLWINDOW
                    return parametros;
                }
            }

            public void Mostrar(string mensagem, TipoFeedback tipo)
            {
                texto.Text = mensagem;
                switch (tipo)
                {
                    case TipoFeedback.Sucesso:
                        faixa.BackColor = Color.FromArgb(31, 157, 115);
                        break;
                    case TipoFeedback.Atencao:
                    case TipoFeedback.Erro:
                        faixa.BackColor = Color.FromArgb(210, 70, 75);
                        break;
                    default:
                        faixa.BackColor = VisualUtils.Azul;
                        break;
                }

                Posicionar();
                if (!Visible)
                    Show(proprietaria);
                temporizador.Stop();
                temporizador.Start();
                BringToFront();
            }

            private void ProprietariaAlterada(object sender, EventArgs e)
            {
                if (Visible)
                    Posicionar();
            }

            private void Posicionar()
            {
                Rectangle area = proprietaria.RectangleToScreen(proprietaria.ClientRectangle);
                int largura = Math.Min(520, Math.Max(320, area.Width - 40));
                Size = new Size(largura, 76);
                Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + 18);
            }
        }
    }
}
