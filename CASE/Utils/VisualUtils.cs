
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CASE.Utils
{
    public static class VisualUtils
    {
        public static readonly Color Azul = Color.FromArgb(23, 105, 210);
        public static readonly Color Marinho = Color.FromArgb(16, 43, 73);
        public static readonly Color Texto = Color.FromArgb(30, 51, 75);
        public static readonly Color TextoSuave = Color.FromArgb(115, 132, 151);
        public static readonly Color Fundo = Color.FromArgb(246, 248, 252);
        public static readonly Color Borda = Color.FromArgb(228, 234, 242);

        private const int EmSetMargins = 0x00D3;
        private const int EmSetCueBanner = 0x1501;
        private const int EcLeftMargin = 0x0001;
        private const int EcRightMargin = 0x0002;

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr janela, string tema, string lista);

        public static void EstilizarRolagem(Control controle)
        {
            controle.HandleCreated += (s, e) => SetWindowTheme(controle.Handle, "DarkMode_Explorer", null);
            controle.VisibleChanged += (s, e) =>
            {
                if (controle.Visible && controle.IsHandleCreated)
                    SetWindowTheme(controle.Handle, "DarkMode_Explorer", null);
            };
            if (controle.IsHandleCreated)
                SetWindowTheme(controle.Handle, "DarkMode_Explorer", null);
        }

        private static readonly ConditionalWeakTable<TextBox, EstadoVisualCampo> EstadosCampos =
            new ConditionalWeakTable<TextBox, EstadoVisualCampo>();
        private static readonly ConditionalWeakTable<DataGridView, EstadoVazioGrade> EstadosVaziosGrade =
            new ConditionalWeakTable<DataGridView, EstadoVazioGrade>();

        private sealed class EstadoVisualCampo
        {
            public bool TemErro { get; set; }
            public bool EstiloAplicado { get; set; }
            public Control ParentEstilo { get; set; }
            public bool PesquisaAplicada { get; set; }
        }

        private sealed class EstadoVazioGrade
        {
            public string Mensagem { get; set; }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(
            IntPtr janela,
            int mensagem,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport(
            "user32.dll",
            EntryPoint = "SendMessageW",
            CharSet = CharSet.Unicode,
            ExactSpelling = true)]
        private static extern IntPtr SendMessageTexto(
            IntPtr janela,
            int mensagem,
            IntPtr wParam,
            string lParam);

        public static PictureBox CriarEspacoIcone(string nome, Rectangle area, Color fundo)
        {
            var icone = new PictureBox { Name = nome, Bounds = area, BackColor = fundo,
                SizeMode = PictureBoxSizeMode.Zoom, TabStop = false, AccessibleName = "Espaço para ícone" };
            Arredondar(icone, 8);
            icone.SizeChanged += (s, e) => Arredondar(icone, 8);
            return icone;
        }

        private sealed class EstadoBotao
        {
            public bool Principal;
        }

        private static readonly ConditionalWeakTable<Button, EstadoBotao> EstadosBotoes =
            new ConditionalWeakTable<Button, EstadoBotao>();

        public static void EstilizarBotao(Button botao)
        {
            EstadoBotao estado;
            if (!EstadosBotoes.TryGetValue(botao, out estado))
            {
                estado = new EstadoBotao();
                estado.Principal = botao.ForeColor.ToArgb() == Color.White.ToArgb() ||
                    botao.BackColor.ToArgb() == Azul.ToArgb();
                EstadosBotoes.Add(botao, estado);
                botao.EnabledChanged += (s, e) => AtualizarCoresBotao(botao, estado);
            }
            botao.FlatStyle = FlatStyle.Flat;
            botao.UseVisualStyleBackColor = false;
            FontStyle estilo = FontStyle.Regular;
            if (estado.Principal)
            {
                estilo = FontStyle.Bold;
            }
            botao.Font = new Font("Segoe UI", 9.5F, estilo);
            botao.TextAlign = ContentAlignment.MiddleCenter;
            AtualizarCoresBotao(botao, estado);
            Arredondar(botao, Math.Min(8, Math.Max(2, botao.Height / 4)));
            AcabamentoArredondado.DefinirBorda(botao, !estado.Principal);
        }

        private static void AtualizarCoresBotao(Button botao, EstadoBotao estado)
        {
            botao.FlatAppearance.BorderSize = 1;
            botao.FlatAppearance.BorderColor = Borda;
            botao.Cursor = Cursors.Hand;
            if (!botao.Enabled)
            {
                botao.BackColor = Color.FromArgb(235, 239, 244);
                botao.ForeColor = Color.FromArgb(115, 125, 140);
                botao.Cursor = Cursors.Default;
            }
            else if (estado.Principal)
            {
                botao.BackColor = Azul;
                botao.ForeColor = Color.White;
                botao.FlatAppearance.BorderSize = 0;
                botao.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 91, 181);
                botao.FlatAppearance.MouseDownBackColor = Color.FromArgb(17, 76, 154);
            }
            else
            {
                botao.BackColor = Color.White;
                botao.ForeColor = Texto;
                botao.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 246, 252);
                botao.FlatAppearance.MouseDownBackColor = Color.FromArgb(231, 239, 249);
            }
        }
        public static void EstilizarCampo(TextBox campo)
        {
            if (campo == null || campo.Parent == null)
            {
                return;
            }

            EstadoVisualCampo estadoCampo = EstadosCampos.GetOrCreateValue(campo);

            if (estadoCampo.EstiloAplicado &&
                ReferenceEquals(estadoCampo.ParentEstilo, campo.Parent))
            {
                return;
            }

            estadoCampo.EstiloAplicado = true;
            estadoCampo.ParentEstilo = campo.Parent;

            campo.AutoSize = false;
            campo.ForeColor = Texto;
            campo.Cursor = Cursors.IBeam;
            campo.TextAlign = HorizontalAlignment.Left;

            Control parent = campo.Parent;
            Panel painelCampo = parent as Panel;
            bool painelSomenteComCampo = painelCampo != null && painelCampo.Controls.Count == 1;

            if (painelSomenteComCampo)
            {
                painelCampo.Padding = new Padding(10, 2, 10, 2);
                painelCampo.Cursor = Cursors.IBeam;
                campo.Margin = Padding.Empty;
                campo.BorderStyle = BorderStyle.None;
                campo.BackColor = painelCampo.BackColor;

                if (campo.Multiline)
                {
                    campo.Dock = DockStyle.Fill;
                }
                else
                {
                    campo.Dock = DockStyle.None;
                    campo.AutoSize = true;
                    campo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    painelCampo.Layout += (s, e) => CentralizarLinhaNoPainel(campo, painelCampo);
                    campo.FontChanged += (s, e) => CentralizarLinhaNoPainel(campo, painelCampo);
                    campo.DpiChangedAfterParent += (s, e) => CentralizarLinhaNoPainel(campo, painelCampo);
                    CentralizarLinhaNoPainel(campo, painelCampo);
                }

                Arredondar(painelCampo, 7);
                AplicarBorda(painelCampo);

                painelCampo.Paint += (s, e) =>
                {
                    if (!campo.Visible)
                    {
                        return;
                    }

                    bool temErro = estadoCampo.TemErro;

                    if (!temErro && !campo.Focused)
                    {
                        return;
                    }

                    Color corBorda = Color.FromArgb(205, 65, 75);

                    if (!temErro)
                    {
                        corBorda = Azul;
                    }

                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath caminho = CriarRetanguloArredondado(
                        new Rectangle(1, 1, painelCampo.ClientSize.Width - 2, painelCampo.ClientSize.Height - 2),
                        7))
                    using (Pen caneta = new Pen(corBorda, 1.5F))
                    {
                        e.Graphics.DrawPath(caneta, caminho);
                    }
                };

                campo.Enter += (s, e) => painelCampo.Invalidate();
                campo.Leave += (s, e) => painelCampo.Invalidate();
                painelCampo.MouseDown += (s, e) => campo.Focus();
            }
            else
            {
                Panel novoPainelCampo = EnvolverCampoEmPainel(campo);
                if (novoPainelCampo != null)
                {
                    EstilizarCampo(campo);
                    return;
                }
                campo.BorderStyle = BorderStyle.None;
                campo.BackColor = Color.White;
            }

            campo.HandleCreated += (s, e) => AplicarMargensInternas(campo);
            if (campo.IsHandleCreated)
            {
                AplicarMargensInternas(campo);
            }
        }

        private static Panel EnvolverCampoEmPainel(TextBox campo)
        {
            Control origem = campo.Parent;
            if (origem == null || EstadosCampos.GetOrCreateValue(campo).PesquisaAplicada)
                return null;

            int indice = origem.Controls.GetChildIndex(campo);
            Rectangle limites = campo.Bounds;
            AnchorStyles ancora = campo.Anchor;
            DockStyle encaixe = campo.Dock;
            Padding margem = campo.Margin;
            int tabIndex = campo.TabIndex;
            TableLayoutPanel tabela = origem as TableLayoutPanel;
            TableLayoutPanelCellPosition celula = tabela == null
                ? new TableLayoutPanelCellPosition(-1, -1)
                : tabela.GetCellPosition(campo);
            int colunaSpan = tabela == null ? 1 : tabela.GetColumnSpan(campo);
            int linhaSpan = tabela == null ? 1 : tabela.GetRowSpan(campo);
            string nome = string.IsNullOrWhiteSpace(campo.Name) ? "Campo" : campo.Name;
            Panel painelCampo = new Panel
            {
                Name = "panelCampo" + nome,
                Bounds = limites,
                Anchor = ancora,
                Dock = encaixe,
                Margin = margem,
                Padding = new Padding(10, 2, 10, 2),
                BackColor = Color.White,
                TabIndex = tabIndex,
                Cursor = Cursors.IBeam
            };

            origem.SuspendLayout();
            try
            {
                origem.Controls.Remove(campo);
                origem.Controls.Add(painelCampo);
                origem.Controls.SetChildIndex(painelCampo, Math.Min(indice, origem.Controls.Count - 1));
                if (tabela != null && celula.Column >= 0 && celula.Row >= 0)
                {
                    tabela.SetCellPosition(painelCampo, celula);
                    tabela.SetColumnSpan(painelCampo, colunaSpan);
                    tabela.SetRowSpan(painelCampo, linhaSpan);
                }
                painelCampo.Controls.Add(campo);
                campo.Dock = DockStyle.Fill;
                campo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                campo.Margin = Padding.Empty;
                campo.BorderStyle = BorderStyle.None;
                campo.BackColor = painelCampo.BackColor;
                campo.AutoSize = false;
            }
            finally
            {
                origem.ResumeLayout(true);
            }

            return painelCampo;
        }

        public static TextBox AdicionarPesquisa(Control tela, string nome, string placeholder,
            Action<string> pesquisar, int margemDireita = 30)
        {
            TextBox campo = new TextBox
            {
                Name = nome,
                Bounds = new Rectangle(tela.Width - margemDireita - 300, 20, 300, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("Segoe UI", 10F),
                TabIndex = 0
            };
            tela.Controls.Add(campo);
            Panel painel = EstilizarPesquisa(campo, placeholder);
            painel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            int larguraPreferida = painel.Width;
            Action posicionar = () =>
            {
                int larguraDisponivel = Math.Max(180, tela.ClientSize.Width - margemDireita - 60);
                painel.Width = Math.Min(larguraPreferida, larguraDisponivel);
                painel.Left = Math.Max(30, tela.ClientSize.Width - margemDireita - painel.Width);
                painel.BringToFront();
            };
            tela.ClientSizeChanged += (s, e) => posicionar();
            posicionar();
            campo.TextChanged += (s, e) => pesquisar(campo.Text.Trim());
            return campo;
        }

        public static Panel EstilizarPesquisa(TextBox campo, string placeholder)
        {
            if (campo == null || campo.Parent == null)
            {
                throw new ArgumentException("O campo de pesquisa precisa pertencer a uma tela.", nameof(campo));
            }

            EstadoVisualCampo estado = EstadosCampos.GetOrCreateValue(campo);
            if (estado.PesquisaAplicada)
            {
                return (Panel)campo.Parent;
            }

            Panel anterior = campo.Parent as Panel;
            if (anterior != null && anterior.Name == "panelCampo" + campo.Name && anterior.Parent != null)
            {
                Control destino = anterior.Parent;
                int ordem = destino.Controls.GetChildIndex(anterior);
                Rectangle limites = anterior.Bounds;
                AnchorStyles ancora = anterior.Anchor;
                DockStyle encaixe = anterior.Dock;
                destino.SuspendLayout();
                anterior.Controls.Remove(campo);
                destino.Controls.Remove(anterior);
                destino.Controls.Add(campo);
                campo.Dock = encaixe;
                campo.Anchor = ancora;
                campo.AutoSize = false;
                campo.Bounds = limites;
                destino.Controls.SetChildIndex(campo, ordem);
                anterior.Dispose();
                destino.ResumeLayout(true);
            }

            Control origem = campo.Parent;
            int indice = origem.Controls.GetChildIndex(campo);
            Panel painel = new Panel
            {
                Name = "panelPesquisa_" + campo.Name,
                Bounds = new Rectangle(campo.Left, campo.Top, campo.Width, Math.Max(40, campo.Height)),
                Anchor = campo.Anchor,
                Dock = campo.Dock,
                Margin = campo.Margin,
                TabIndex = campo.TabIndex,
                BackColor = Color.FromArgb(247, 249, 252),
                Cursor = Cursors.IBeam
            };
            PictureBox icone = new PictureBox
            {
                Name = "iconePesquisa_" + campo.Name,
                Image = Properties.Resources.LucidePesquisa,
                Size = new Size(20, 20),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                TabStop = false,
                Cursor = Cursors.IBeam
            };

            origem.SuspendLayout();
            origem.Controls.Add(painel);
            painel.Controls.Add(campo);
            painel.Controls.Add(icone);
            origem.Controls.SetChildIndex(painel, indice);
            painel.BringToFront();
            estado.PesquisaAplicada = true;
            estado.EstiloAplicado = true;
            estado.ParentEstilo = painel;
            campo.Dock = DockStyle.None;
            campo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            campo.AutoSize = true;
            campo.TabIndex = 0;
            campo.BorderStyle = BorderStyle.None;
            campo.BackColor = painel.BackColor;
            campo.ForeColor = Texto;
            campo.AccessibleName = placeholder;

            Action ajustar = () =>
            {
                icone.Location = new Point(12, Math.Max(0, (painel.ClientSize.Height - icone.Height) / 2));
                campo.SetBounds(40, Math.Max(0, (painel.ClientSize.Height - campo.PreferredHeight) / 2),
                    Math.Max(1, painel.ClientSize.Width - 52), campo.PreferredHeight);
                Arredondar(painel, 12);
            };
            painel.Layout += (s, e) => ajustar();
            campo.FontChanged += (s, e) => ajustar();
            campo.DpiChangedAfterParent += (s, e) => ajustar();
            painel.MouseDown += (s, e) => campo.Focus();
            icone.Click += (s, e) => campo.Focus();
            campo.Enter += (s, e) => painel.Invalidate();
            campo.Leave += (s, e) => painel.Invalidate();
            AplicarBorda(painel);
            DefinirPlaceholder(campo, placeholder);
            ajustar();
            origem.ResumeLayout(true);
            return painel;
        }

        private static void CentralizarLinhaNoPainel(TextBox campo, Panel painel)
        {
            if (campo == null || painel == null || !ReferenceEquals(campo.Parent, painel) || campo.Multiline)
            {
                return;
            }

            int altura = campo.PreferredHeight;
            int largura = Math.Max(0, painel.ClientSize.Width - painel.Padding.Horizontal);
            int topo = Math.Max(0, (painel.ClientSize.Height - altura) / 2);
            Rectangle area = new Rectangle(painel.Padding.Left, topo, largura, altura);

            if (campo.Bounds != area)
            {
                campo.Bounds = area;
            }
        }

        public static void DefinirPlaceholder(TextBox campo, string texto)
        {
            if (campo == null || string.IsNullOrWhiteSpace(texto))
            {
                return;
            }

            EventHandler aplicarPlaceholder = null;
            aplicarPlaceholder = (s, e) =>
            {
                if (!campo.IsHandleCreated)
                {
                    return;
                }

                SendMessageTexto(campo.Handle, EmSetCueBanner, new IntPtr(1), texto);
                campo.HandleCreated -= aplicarPlaceholder;
            };

            campo.HandleCreated += aplicarPlaceholder;
            if (campo.IsHandleCreated)
            {
                aplicarPlaceholder(campo, EventArgs.Empty);
            }
        }

        public static void DefinirEstadoErro(TextBox campo, bool temErro)
        {
            if (campo == null)
            {
                return;
            }

            EstadoVisualCampo estado = EstadosCampos.GetOrCreateValue(campo);
            estado.TemErro = temErro;

            if (campo.Parent != null)
            {
                campo.Parent.Invalidate();
            }
        }

        private static void AplicarMargensInternas(TextBox campo)
        {
            int margem = 8;
            int margens = margem | (margem << 16);
            SendMessage(
                campo.Handle,
                EmSetMargins,
                new IntPtr(EcLeftMargin | EcRightMargin),
                new IntPtr(margens));
        }

        public static void EstilizarSelecao(ComboBox campo)
        {
            if (campo == null)
            {
                return;
            }

            // A pintura nativa mantém seta, valor selecionado e foco alinhados pelo Windows.
            campo.DrawMode = DrawMode.Normal;
            campo.FlatStyle = FlatStyle.Standard;
            campo.BackColor = Color.White;
            campo.ForeColor = Texto;
            if (campo.DropDownStyle == ComboBoxStyle.DropDownList)
            {
                campo.Cursor = Cursors.Hand;
            }
            else
            {
                campo.Cursor = Cursors.IBeam;
            }
            campo.MaxDropDownItems = 12;
            campo.IntegralHeight = true;
            campo.DpiChangedAfterParent += (s, e) => AtualizarAlturaLista(campo);
            Panel painel = campo.Parent as Panel;
            if (painel != null && painel.Controls.Count == 1)
            {
                campo.Dock = DockStyle.None;
                campo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                painel.Cursor = Cursors.Hand;
                painel.Layout += (s, e) => CentralizarSelecaoNoPainel(campo, painel);
                campo.FontChanged += (s, e) => CentralizarSelecaoNoPainel(campo, painel);
                painel.MouseDown += (s, e) =>
                {
                    if (campo.Enabled)
                    {
                        campo.Focus();
                        campo.DroppedDown = true;
                    }
                };
                CentralizarSelecaoNoPainel(campo, painel);
            }
            AtualizarAlturaLista(campo);
        }

        private static void CentralizarSelecaoNoPainel(ComboBox campo, Panel painel)
        {
            int altura = campo.PreferredHeight;
            Rectangle area = new Rectangle(painel.Padding.Left,
                Math.Max(0, (painel.ClientSize.Height - altura) / 2),
                Math.Max(0, painel.ClientSize.Width - painel.Padding.Horizontal), altura);
            if (campo.Bounds != area)
            {
                campo.Bounds = area;
            }
        }

        public static bool LinhaCorrespondePesquisa(DataGridViewRow linha, string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return true;
            }
            foreach (DataGridViewCell celula in linha.Cells)
            {
                if (Convert.ToString(celula.FormattedValue).IndexOf(texto,
                    StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static void FiltrarGrades(Control raiz, string texto)
        {
            foreach (Control controle in raiz.Controls)
            {
                DataGridView grade = controle as DataGridView;
                if (grade != null)
                {
                    System.Data.DataTable tabela = grade.DataSource as System.Data.DataTable;
                    if (tabela != null)
                    {
                        // As consultas já retornam somente o escopo autorizado. Este filtro é local.
                        string valor = texto.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]");
                        var condicoes = new System.Collections.Generic.List<string>();
                        foreach (System.Data.DataColumn coluna in tabela.Columns)
                        {
                            condicoes.Add("CONVERT([" + coluna.ColumnName.Replace("]", "\\]") +
                                "], 'System.String') LIKE '%" + valor + "%'");
                        }
                        tabela.DefaultView.RowFilter = string.Join(" OR ", condicoes);
                    }
                    else if (grade.DataSource == null)
                    {
                        foreach (DataGridViewRow linha in grade.Rows)
                        {
                            if (linha.IsNewRow)
                            {
                                continue;
                            }
                            bool corresponde = string.IsNullOrWhiteSpace(texto);
                            foreach (DataGridViewCell celula in linha.Cells)
                            {
                                if (Convert.ToString(celula.FormattedValue).IndexOf(texto,
                                    StringComparison.CurrentCultureIgnoreCase) >= 0)
                                {
                                    corresponde = true;
                                    break;
                                }
                            }
                            linha.Visible = corresponde;
                        }
                    }
                }
                else
                {
                    FiltrarGrades(controle, texto);
                }
            }
        }

        private static void AtualizarAlturaLista(ComboBox campo)
        {
            int dpi = campo.DeviceDpi;

            if (dpi <= 0)
            {
                dpi = 96;
            }

            campo.DropDownHeight = (int)Math.Round(240F * dpi / 96F);
        }
        public static void AplicarIcone(Button botao, Image imagem)
        {
            botao.Image = imagem;
            botao.ImageAlign = ContentAlignment.MiddleLeft;
            botao.TextImageRelation = TextImageRelation.ImageBeforeText;
            botao.Padding = new Padding(10, 0, 10, 0);
        }

        public static void ExibirEstadoVazio(DataGridView grade, string mensagem)
        {
            if (grade == null)
                return;

            EstadoVazioGrade estado;
            if (EstadosVaziosGrade.TryGetValue(grade, out estado))
            {
                estado.Mensagem = mensagem ?? string.Empty;
                grade.Invalidate();
                return;
            }

            estado = new EstadoVazioGrade { Mensagem = mensagem ?? string.Empty };
            EstadosVaziosGrade.Add(grade, estado);
            grade.Paint += (s, e) =>
            {
                DataGridView atual = s as DataGridView;
                EstadoVazioGrade estadoAtual;
                if (atual == null || !EstadosVaziosGrade.TryGetValue(atual, out estadoAtual))
                    return;
                foreach (DataGridViewRow linha in atual.Rows)
                    if (linha.Visible && !linha.IsNewRow) return;

                var area = new Rectangle(16, atual.ColumnHeadersHeight + 16,
                    Math.Max(0, atual.ClientSize.Width - 32), Math.Max(0, atual.ClientSize.Height - atual.ColumnHeadersHeight - 32));
                TextRenderer.DrawText(e.Graphics, estadoAtual.Mensagem, atual.Font, area, TextoSuave,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
            };
            grade.RowsAdded += (s, e) => ((DataGridView)s).Invalidate();
            grade.RowsRemoved += (s, e) => ((DataGridView)s).Invalidate();
        }

        public static GraphicsPath CriarRetanguloArredondado(Rectangle area, int raio)
        {
            return CriarRetanguloArredondado((RectangleF)area, raio);
        }

        public static GraphicsPath CriarRetanguloArredondado(RectangleF area, float raio)
        {
            var caminho = new GraphicsPath();
            if (area.Width <= 0 || area.Height <= 0) return caminho;

            float diametro = Math.Min(Math.Max(0, raio * 2), Math.Min(area.Width, area.Height));
            if (diametro == 0)
            {
                caminho.AddRectangle(area);
                return caminho;
            }

            caminho.AddArc(area.Left, area.Top, diametro, diametro, 180, 90);
            caminho.AddArc(area.Right - diametro, area.Top, diametro, diametro, 270, 90);
            caminho.AddArc(area.Right - diametro, area.Bottom - diametro, diametro, diametro, 0, 90);
            caminho.AddArc(area.Left, area.Bottom - diametro, diametro, diametro, 90, 90);
            caminho.CloseFigure();
            return caminho;
        }

        public static void Arredondar(Control controle, int raio)
        {
            AcabamentoArredondado.Aplicar(controle, raio);
        }

        public static void AplicarBorda(Control controle)
        {
            AcabamentoArredondado.DefinirBorda(controle, true);
        }
    }
}
