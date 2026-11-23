using System;
using System.Data;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE.Quermesse
{
    public partial class FinanceiroControl : UserControl
    {
        private readonly Usuario usuarioAutenticado;
        private readonly DespesaService despesas = new DespesaService();
        private bool carregando = true;
        private readonly DataGridView conferencias = new DataGridView();
        private readonly TabControl abasFinanceiro = new TabControl();
        private readonly Panel guiaFinanceiro = new Panel();
        private readonly Label resumoConferencias = new Label();
        private bool podeRegistrarDespesa;
        private bool ajudaExpandida;
        private readonly Label ajudaFinanceiro = new Label();
        private readonly Button botaoAjuda = new Button();

        public FinanceiroControl() : this(null, null)
        {
        }

        public FinanceiroControl(Usuario usuario, Evento evento)
        {
            InitializeComponent();
            usuarioAutenticado = usuario;
            FrontendTheme.Aplicar(this);
            dtpFinanceiro.Value = evento == null ? DateTime.Today :
                DateTime.Today < evento.DataInicio.Date ? evento.DataInicio.Date :
                DateTime.Today > evento.DataFim.Date ? evento.DataFim.Date : DateTime.Today;
            cmbFiltroFinanceiro.Items.Clear();
            cmbSetorCusto.Items.Clear();
            cmbFiltroFinanceiro.DisplayMember = "Nome";
            cmbSetorCusto.DisplayMember = "Nome";
            lblSetor.Text = "Evento";
            lblSetor2.Text = "Evento";
            cmbSetorCusto.Enabled = false;
            lblSubtitulo.Text = "Veja quanto foi vendido e gasto. Depois, confira se os valores recebidos correspondem às vendas.";
            lblReceitas.Text = "Vendas registradas no dia";
            lblCustos.Text = "Despesas registradas no dia";
            lblResultadoparcial.Text = "Resultado parcial do dia";
            lblDescricaodocusto.Text = "Descrição da despesa *";
            btnAdicionarCusto.Text = "+ Registrar despesa";
            colCustosSetor.HeaderText = "Evento";
            colCustosResponsavel.HeaderText = "Responsável (ID)";
            lblSetor2.Visible = cmbSetorCusto.Visible = false;
            lblDescricaodocusto.Text = "Descrição da despesa *";
            Control contornoDescricao = txtDescricaoCusto.Parent is Panel painelDescricao &&
                painelDescricao.Controls.Count == 1 && painelDescricao.Controls[0] == txtDescricaoCusto
                ? (Control)painelDescricao
                : txtDescricaoCusto;
            contornoDescricao.Width = 510;
            PrepararAbasFinanceiro();
            PrepararOrientacao();
            txtDescricaoCusto.TextChanged += (s, e) => AtualizarBotaoDespesa();
            numValorCusto.ValueChanged += (s, e) => AtualizarBotaoDespesa();
            VisualUtils.DefinirPlaceholder(txtDescricaoCusto, "Ex.: compra de gelo ou aluguel de mesas");
            lblValorFinanceiroResumo0.Text = "—";
            lblValorFinanceiroResumo1.Text = "—";
            lblValorFinanceiroResumo2.Text = "—";
            lblDatadocustohoje.Text = "Data selecionada no filtro";
            labelAviso.Text = "Selecione um evento autorizado.";
            btnAdicionarCusto.Enabled = false;
            txtDescricaoCusto.MaxLength = 255;
            Button apurar = new Button();
            apurar.Name = "btnAbrirApuracao";
            apurar.Text = "Conferir valores recebidos";
            apurar.BackColor = VisualUtils.Azul;
            apurar.ForeColor = System.Drawing.Color.White;
            apurar.SetBounds(700, 32, 240, 36);
            VisualUtils.EstilizarBotao(apurar);
            panelFiltrosFinanceiro.Controls.Add(apurar);
            apurar.Click += AbrirApuracao;
            if (usuario == null)
            {
                carregando = false;
                return;
            }
            try
            {
                foreach (Evento permitido in new EventoService().Listar(usuario))
                {
                    if (evento != null && permitido.IdEvento != evento.IdEvento)
                        continue;
                    cmbFiltroFinanceiro.Items.Add(permitido);
                    cmbSetorCusto.Items.Add(permitido);
                    if (evento != null && permitido.IdEvento == evento.IdEvento)
                    {
                        cmbFiltroFinanceiro.SelectedItem = permitido;
                        cmbSetorCusto.SelectedItem = permitido;
                    }
                }
                if (cmbFiltroFinanceiro.SelectedIndex < 0 && cmbFiltroFinanceiro.Items.Count > 0)
                {
                    cmbFiltroFinanceiro.SelectedIndex = 0;
                    cmbSetorCusto.SelectedIndex = 0;
                }
                carregando = false;
                AtualizarDados();
            }
            catch (Exception erro)
            {
                ExibirErro(erro);
            }
            finally
            {
                carregando = false;
            }
            if (evento != null)
            {
                cmbFiltroFinanceiro.Visible = false;
                lblSetor.Visible = false;
                var contexto = new Label
                {
                    Text = "Evento em consulta\n" + evento.Nome,
                    ForeColor = VisualUtils.Marinho, AutoEllipsis = true,
                    Font = new System.Drawing.Font("Segoe UI", 10),
                    Bounds = new System.Drawing.Rectangle(18, 12, 280, 56)
                };
                panelFiltrosFinanceiro.Controls.Add(contexto);
            }
            AjustarIndicadores();
        }

        private void AtualizarDados()
        {

            Evento evento = cmbFiltroFinanceiro.SelectedItem as Evento;
            if (usuarioAutenticado == null || evento == null)
            {
                return;
            }
            decimal totalDespesas = 0;
            decimal totalVendas = 0;
            bool precosPendentes = false;
            var despesasDoEvento = despesas.ListarPorEvento(usuarioAutenticado, evento.IdEvento);
            DataTable vendas = new ConsultaOperacionalService().Registros(usuarioAutenticado,
                evento.IdEvento, dtpFinanceiro.Value, dtpFinanceiro.Value);
            dgvCustos.Rows.Clear();
            foreach (Despesa despesa in despesasDoEvento)
            {
                if (despesa.Data.Date != dtpFinanceiro.Value.Date)
                {
                    continue;
                }
                totalDespesas += despesa.Valor;
                dgvCustos.Rows.Add(despesa.Data.ToString("dd/MM/yyyy"), despesa.Descricao,
                    evento.Nome, despesa.Valor.ToString("C2"), despesa.IdUsuarioResponsavel);
            }
            foreach (DataRow venda in vendas.Rows)
            {
                if (Convert.ToString(venda["tipo"]) == "venda")
                {
                    if (venda.IsNull("total"))
                    {
                        precosPendentes = true;
                        continue;
                    }
                    totalVendas += Convert.ToDecimal(venda["total"]);
                }
            }
            lblValorFinanceiroResumo0.Text = totalVendas.ToString("C2");
            lblValorFinanceiroResumo1.Text = totalDespesas.ToString("C2");
            lblValorFinanceiroResumo2.Text = (totalVendas - totalDespesas).ToString("C2");
            lblDatadocustohoje.Text = "Despesa em " + dtpFinanceiro.Value.ToString("dd/MM/yyyy") + " para o evento selecionado.";
            labelAviso.Text = "Vendas − despesas é um resultado parcial, não o dinheiro conferido nem o lucro líquido.";
            if (precosPendentes)
            {
                lblValorFinanceiroResumo0.Text = "Indisponível";
                lblValorFinanceiroResumo2.Text = "Indisponível";
                labelAviso.Text = "Há vendas antigas sem preço praticado. Não é possível informar uma receita completa.";
            }
            podeRegistrarDespesa = !string.Equals(evento.Situacao, SituacoesEvento.Cancelado, StringComparison.OrdinalIgnoreCase);
            AtualizarBotaoDespesa();
            VisualUtils.ExibirEstadoVazio(dgvCustos, "Nenhuma despesa registrada nesta data.");
            AtualizarConferencias(evento);
        }

        private void PrepararAbasFinanceiro()
        {
            abasFinanceiro.Name = "abasFinanceiro";
            abasFinanceiro.Dock = DockStyle.Fill;
            var abaDespesas = new TabPage("Despesas") { BackColor = System.Drawing.Color.White, Padding = new Padding(12) };
            var abaConferencias = new TabPage("Conferências do dia");
            abaConferencias.Text = "Recebimentos por setor";
            abaConferencias.BackColor = System.Drawing.Color.White;
            abaConferencias.Padding = new Padding(12);
            abasFinanceiro.Font = new System.Drawing.Font("Segoe UI", 10);
            abasFinanceiro.Padding = new System.Drawing.Point(16, 10);
            abasFinanceiro.DrawMode = TabDrawMode.OwnerDrawFixed;
            abasFinanceiro.ItemSize = new System.Drawing.Size(220, 42);
            abasFinanceiro.SizeMode = TabSizeMode.Fixed;
            abasFinanceiro.DrawItem += (s, e) =>
            {
                bool selecionada = e.Index == abasFinanceiro.SelectedIndex;
                using (var fundo = new System.Drawing.SolidBrush(selecionada
                    ? System.Drawing.Color.FromArgb(234, 242, 254) : System.Drawing.Color.White))
                    e.Graphics.FillRectangle(fundo, e.Bounds);
                TextRenderer.DrawText(e.Graphics, abasFinanceiro.TabPages[e.Index].Text,
                    abasFinanceiro.Font, e.Bounds, selecionada ? VisualUtils.Azul : VisualUtils.TextoSuave,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                if (selecionada)
                    using (var linha = new System.Drawing.Pen(VisualUtils.Azul, 3))
                        e.Graphics.DrawLine(linha, e.Bounds.Left + 12, e.Bounds.Bottom - 2, e.Bounds.Right - 12, e.Bounds.Bottom - 2);
            };
            abasFinanceiro.SelectedIndexChanged += (s, e) => abasFinanceiro.Invalidate();
            panelCustos.Controls.Remove(dgvCustos);
            dgvCustos.Dock = DockStyle.Fill;
            var layoutDespesas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            layoutDespesas.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            layoutDespesas.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panelCorpo.Controls.Remove(panelNovoCusto);
            panelNovoCusto.Dock = DockStyle.Fill;
            panelNovoCusto.Margin = Padding.Empty;
            layoutDespesas.Controls.Add(panelNovoCusto, 0, 0);
            layoutDespesas.Controls.Add(dgvCustos, 0, 1);
            abaDespesas.Controls.Add(layoutDespesas);
            conferencias.Name = "dgvConferenciasDia";
            conferencias.Dock = DockStyle.Fill;
            conferencias.ReadOnly = true;
            conferencias.AllowUserToAddRows = false;
            conferencias.AllowUserToDeleteRows = false;
            conferencias.RowHeadersVisible = false;
            conferencias.BackgroundColor = System.Drawing.Color.White;
            conferencias.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            foreach (string coluna in new[] { "Setor / turno", "Vendas esperadas", "Último valor conferido", "Diferença atual", "Situação", "Conferido em" })
                conferencias.Columns.Add(coluna, coluna);
            abaConferencias.Controls.Add(conferencias);
            resumoConferencias.Dock = DockStyle.Top;
            resumoConferencias.Height = 62;
            resumoConferencias.Padding = new Padding(14, 10, 14, 6);
            resumoConferencias.ForeColor = VisualUtils.TextoSuave;
            resumoConferencias.Font = new System.Drawing.Font("Segoe UI", 10);
            resumoConferencias.BackColor = System.Drawing.Color.White;
            abaConferencias.Controls.Add(resumoConferencias);
            var acaoConferir = new DataGridViewButtonColumn
            {
                Name = "acaoConferir", HeaderText = "Ação", Text = "Conferir",
                UseColumnTextForButtonValue = true, FillWeight = 85
            };
            conferencias.Columns.Add(acaoConferir);
            conferencias.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == acaoConferir.Index)
                    AbrirApuracaoDaJornada(Convert.ToInt32(conferencias.Rows[e.RowIndex].Tag));
            };
            abasFinanceiro.TabPages.Add(abaConferencias);
            abasFinanceiro.TabPages.Add(abaDespesas);
            abasFinanceiro.SelectedIndex = 0;
            panelCustos.Controls.Add(abasFinanceiro);
            FrontendTheme.Aplicar(panelCustos);
            conferencias.EnableHeadersVisualStyles = false;
            conferencias.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            conferencias.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            conferencias.RowTemplate.MinimumHeight = 40;
            conferencias.Columns[0].FillWeight = 175;
            conferencias.Columns[4].FillWeight = 120;
            conferencias.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex != acaoConferir.Index)
                    AbrirApuracaoDaJornada(Convert.ToInt32(conferencias.Rows[e.RowIndex].Tag));
            };
        }

        private void AtualizarConferencias(Evento evento)
        {
            var linhas = new System.Collections.Generic.List<DataGridViewRow>();
            int pendentes = 0;
            int divergencias = 0;
            var jornadas = new System.Collections.Generic.HashSet<int>();
            var service = new ApuracaoService();
            foreach (DataRow escala in new ConsultaOperacionalService().Escalas(usuarioAutenticado, evento.IdEvento).Rows)
            {
                int idJornada = Convert.ToInt32(escala["id_jornada"]);
                if (Convert.ToDateTime(escala["data_jornada"]).Date != dtpFinanceiro.Value.Date || !jornadas.Add(idJornada))
                    continue;
                decimal esperado = 0;
                bool completo = true;
                var modalidades = new System.Collections.Generic.Dictionary<string, decimal>();
                foreach (DataRow venda in service.VendasPorJornada(usuarioAutenticado, idJornada).Rows)
                {
                    string forma = Convert.ToString(venda["forma_pagamento"]).Trim().ToLowerInvariant();
                    if (venda.IsNull("total") || (forma != "dinheiro" && forma != "pix" && forma != "cartao"))
                    {
                        completo = false;
                        continue;
                    }
                    decimal valor = Convert.ToDecimal(venda["total"]);
                    esperado += valor;
                    modalidades[forma] = (modalidades.ContainsKey(forma) ? modalidades[forma] : 0M) + valor;
                }
                Apuracao ultima = null;
                foreach (Apuracao item in service.ListarPorJornada(usuarioAutenticado, idJornada))
                    if (ultima == null || item.IdApuracao > ultima.IdApuracao)
                        ultima = item;
                bool detalhada = ultima != null && ultima.ValorDinheiro.HasValue && ultima.ValorPix.HasValue && ultima.ValorCartao.HasValue;
                bool confere = detalhada && completo &&
                    ultima.ValorDinheiro.Value == (modalidades.ContainsKey("dinheiro") ? modalidades["dinheiro"] : 0M) &&
                    ultima.ValorPix.Value == (modalidades.ContainsKey("pix") ? modalidades["pix"] : 0M) &&
                    ultima.ValorCartao.Value == (modalidades.ContainsKey("cartao") ? modalidades["cartao"] : 0M);
                string situacao = !completo ? "Vendas incompletas" : ultima == null ? "Pendente" :
                    !detalhada ? "Histórico sem modalidades" : confere ? "Valores conferem" : "Com divergência";
                if (ultima == null) pendentes++;
                else if (!confere) divergencias++;
                var linha = new DataGridViewRow();
                linha.CreateCells(conferencias, escala["descricao_jornada"], completo ? esperado.ToString("C2") : "Indisponível",
                    ultima == null ? "Não conferido" : ultima.ValorInformado.ToString("C2"),
                    ultima == null || !completo ? "—" : (ultima.ValorInformado - esperado).ToString("C2"), situacao,
                    ultima == null ? "—" : ultima.DataHora.ToString("dd/MM/yyyy HH:mm"), "Conferir");
                linha.Cells[4].Style.ForeColor = confere ? System.Drawing.Color.SeaGreen : System.Drawing.Color.DarkOrange;
                linha.Tag = idJornada;
                linhas.Add(linha);
            }
            conferencias.Rows.Clear();
            conferencias.Rows.AddRange(linhas.ToArray());
            resumoConferencias.Text = pendentes + " setor(es)/turno(s) sem conferência · " + divergencias + " com valores a revisar.\n" +
                "Clique em Conferir na linha e informe quanto foi recebido em dinheiro, Pix e cartão.";
            if (linhas.Count == 0)
                resumoConferencias.Text = "Sem setores para conferir nesta data.\nSelecione um dia com escala de trabalho para consultar os recebimentos.";
            VisualUtils.ExibirEstadoVazio(conferencias, "Nenhuma jornada com escala nesta data. Escolha outra data para conferir os recebimentos.");
        }

        private void Filtros_Changed(object sender, EventArgs e)
        {

            if (carregando)
            {
                return;
            }
            try
            {
                cmbSetorCusto.SelectedItem = cmbFiltroFinanceiro.SelectedItem;
                AtualizarDados();
            }
            catch (Exception erro)
            {
                btnAdicionarCusto.Enabled = false;
                ExibirErro(erro);
            }
        }

        private void btnAdicionarCusto_Click(object sender, EventArgs e)
        {

            Evento evento = cmbSetorCusto.SelectedItem as Evento;
            if (evento == null)
            {
                labelAviso.Text = "Selecione o evento da despesa.";
                return;
            }
            btnAdicionarCusto.Enabled = false;
            try
            {
                Despesa despesa = new Despesa();
                despesa.IdEvento = evento.IdEvento;
                despesa.Descricao = txtDescricaoCusto.Text;
                despesa.Valor = numValorCusto.Value;
                despesa.Data = dtpFinanceiro.Value.Date;
                int id = despesas.Cadastrar(usuarioAutenticado, despesa);
                txtDescricaoCusto.Clear();
                numValorCusto.Value = 0;
                AtualizarDados();
                abasFinanceiro.SelectedIndex = 1;
                labelAviso.Text = "Despesa registrada. Os totais desta data foram atualizados.";
                FeedbackUsuario.Exibir(this, labelAviso.Text, TipoFeedback.Sucesso);
            }
            catch (Exception erro)
            {
                ExibirErro(erro);
            }
            finally
            {
                AtualizarBotaoDespesa();
            }
        }

        private void AbrirApuracao(object sender, EventArgs e)
        {
            int? jornada = abasFinanceiro.SelectedIndex == 0 && conferencias.SelectedRows.Count > 0
                ? (int?)Convert.ToInt32(conferencias.SelectedRows[0].Tag) : null;
            AbrirApuracaoDaJornada(jornada);
        }

        private void AbrirApuracaoDaJornada(int? idJornada)
        {

            Evento evento = cmbFiltroFinanceiro.SelectedItem as Evento;
            if (evento == null || usuarioAutenticado == null)
            {
                return;
            }
            using (Form janela = new Form())
            {
                IconoAplicacao.Aplicar(janela);
                janela.Name = "ApuracaoEventoForm";
                janela.Text = "Conferir recebimentos";
                janela.ClientSize = new System.Drawing.Size(1100,
                    Math.Min(880, Screen.FromControl(this).WorkingArea.Height - 80));
                janela.MinimumSize = new System.Drawing.Size(1040, 650);
                janela.StartPosition = FormStartPosition.CenterParent;
                ApuracaoControl controle = new ApuracaoControl(usuarioAutenticado, evento, dtpFinanceiro.Value.Date);
                if (idJornada.HasValue)
                    controle.SelecionarJornada(idJornada.Value);
                controle.Dock = DockStyle.Fill;
                janela.Controls.Add(controle);
                janela.ShowDialog(this);
                if (controle.DataConferida.HasValue)
                {
                    carregando = true;
                    dtpFinanceiro.Value = controle.DataConferida.Value;
                    carregando = false;
                    abasFinanceiro.SelectedIndex = 0;
                }
            }
            try
            {
                AtualizarDados();
            }
            catch (Exception erro)
            {
                ExibirErro(erro);
            }
        }

        private void ExibirErro(Exception erro)
        {
            CASE.Utils.FeedbackUsuario.ExibirErro(this, erro,
                "Não foi possível consultar ou registrar os dados financeiros.", labelAviso);
            System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em FinanceiroControl: " + erro.StackTrace);
        }

        private void AtualizarBotaoDespesa()
        {
            btnAdicionarCusto.Enabled = podeRegistrarDespesa &&
                !string.IsNullOrWhiteSpace(txtDescricaoCusto.Text) && numValorCusto.Value > 0;
            lblDatadocustohoje.Text = !podeRegistrarDespesa
                ? "Escolha um evento disponível para registrar despesas. Eventos cancelados permitem apenas consulta."
                : "Despesa em " + dtpFinanceiro.Value.ToString("dd/MM/yyyy") +
                    " · Preencha a descrição e um valor maior que zero para salvar.";
        }

        private void PrepararOrientacao()
        {
            guiaFinanceiro.BackColor = VisualUtils.Fundo;
            ajudaFinanceiro.Text = "As vendas são calculadas a partir de Vendas e perdas. Para conferir, informe quanto foi recebido em dinheiro, Pix e cartão por setor/turno.\nNa aba Despesas, registre os gastos do evento. O resultado parcial corresponde às vendas menos as despesas.";
            ajudaFinanceiro.Font = new System.Drawing.Font("Segoe UI", 10);
            ajudaFinanceiro.ForeColor = VisualUtils.TextoSuave;
            ajudaFinanceiro.Visible = false;
            botaoAjuda.Text = "Como funciona?";
            botaoAjuda.BackColor = System.Drawing.Color.White;
            botaoAjuda.ForeColor = VisualUtils.Azul;
            botaoAjuda.SetBounds(0, 0, 165, 36);
            VisualUtils.EstilizarBotao(botaoAjuda);
            botaoAjuda.Click += (s, e) =>
            {
                ajudaExpandida = !ajudaExpandida;
                ajudaFinanceiro.Visible = ajudaExpandida;
                botaoAjuda.Text = ajudaExpandida ? "Ocultar explicação" : "Como funciona?";
                AjustarIndicadores();
            };
            guiaFinanceiro.Controls.Add(botaoAjuda);
            guiaFinanceiro.Controls.Add(ajudaFinanceiro);
            panelCorpo.Controls.Add(guiaFinanceiro);
            VisualUtils.Arredondar(guiaFinanceiro, 12);
            guiaFinanceiro.SizeChanged += (s, e) => VisualUtils.Arredondar(guiaFinanceiro, 12);
            lblData.Text = "Data da consulta";
            lblValorR.Text = "Valor da despesa (R$) *";
            btnAdicionarCusto.Text = "Salvar despesa";
            colCustosDescricao.FillWeight = 180;
            conferencias.Columns[1].HeaderText = "Vendas registradas";
            conferencias.Columns[2].HeaderText = "Recebido informado";
            conferencias.Columns[3].HeaderText = "Recebido − vendas";
            lblDatadocustohoje.Width = 700;
        }

        private void panelCorpo_SizeChanged(object sender, EventArgs e)
        {
            AjustarIndicadores();
        }

        private void AjustarIndicadores()
        {
            int alturaAjuda = ajudaExpandida ? 112 : 38;
            guiaFinanceiro.SetBounds(0, 0, panelCorpo.Width, alturaAjuda);
            ajudaFinanceiro.SetBounds(0, 46, panelCorpo.Width - 16, 66);
            panelFiltrosFinanceiro.SetBounds(0, alturaAjuda + 14, panelCorpo.Width, 82);
            int topoIndicadores = panelFiltrosFinanceiro.Bottom + 16;
            int topoTabela = topoIndicadores + 116;
            panelCustos.SetBounds(0, topoTabela, panelCorpo.Width, Math.Max(300, ClientSize.Height - panelCorpo.Top - topoTabela - 60));
            panelCorpo.Height = panelCustos.Bottom;
            labelAviso.Top = panelCorpo.Bottom + 10;
            labelAviso.Width = panelCorpo.Width;
            AutoScrollMinSize = new System.Drawing.Size(1030, labelAviso.Bottom + 16);
            Panel[] indicadores = { panelFinanceiroResumo0, panelFinanceiroResumo1, panelFinanceiroResumo2 };
            for (int i = 0; i < indicadores.Length; i++)
            {
                indicadores[i].SetBounds(i * (panelCorpo.Width + 15) / 3, topoIndicadores, (panelCorpo.Width - 30) / 3, 100);
            }
        }
    }
}
