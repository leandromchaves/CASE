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

        public FinanceiroControl() : this(null, null)
        {
        }

        public FinanceiroControl(Usuario usuario, Evento evento)
        {
            InitializeComponent();
            usuarioAutenticado = usuario;
            FrontendTheme.Aplicar(this);
            dtpFinanceiro.Value = DateTime.Today;
            cmbFiltroFinanceiro.Items.Clear();
            cmbSetorCusto.Items.Clear();
            cmbFiltroFinanceiro.DisplayMember = "Nome";
            cmbSetorCusto.DisplayMember = "Nome";
            lblSetor.Text = "Evento";
            lblSetor2.Text = "Evento";
            cmbSetorCusto.Enabled = false;
            lblSubtitulo.Text = "Acompanhe as vendas, registre despesas e confira os recebimentos de cada jornada.";
            lblReceitas.Text = "Vendas registradas no dia";
            lblCustos.Text = "Despesas registradas no dia";
            lblResultadoparcial.Text = "Vendas − despesas";
            lblDescricaodocusto.Text = "Descrição da despesa *";
            btnAdicionarCusto.Text = "+ Registrar despesa";
            colCustosSetor.HeaderText = "Evento";
            colCustosResponsavel.HeaderText = "Responsável (ID)";
            lblSetor2.Visible = cmbSetorCusto.Visible = false;
            lblDescricaodocusto.Text = "Descrição da despesa *";
            txtDescricaoCusto.Width = 510;
            PrepararAbasFinanceiro();
            lblValorFinanceiroResumo0.Text = "—";
            lblValorFinanceiroResumo1.Text = "—";
            lblValorFinanceiroResumo2.Text = "—";
            lblDatadocustohoje.Text = "Data selecionada no filtro";
            labelAviso.Text = "Selecione um evento autorizado.";
            btnAdicionarCusto.Enabled = false;
            txtDescricaoCusto.MaxLength = 255;
            Button apurar = new Button();
            apurar.Name = "btnAbrirApuracao";
            apurar.Text = "Conferir recebimentos";
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
            dgvCustos.Rows.Clear();
            foreach (Despesa despesa in despesas.ListarPorEvento(usuarioAutenticado, evento.IdEvento))
            {
                if (despesa.Data.Date != dtpFinanceiro.Value.Date)
                {
                    continue;
                }
                totalDespesas += despesa.Valor;
                dgvCustos.Rows.Add(despesa.Data.ToString("dd/MM/yyyy"), despesa.Descricao,
                    evento.Nome, despesa.Valor.ToString("C2"), despesa.IdUsuarioResponsavel);
            }
            DataTable vendas = new ConsultaOperacionalService().Registros(usuarioAutenticado,
                evento.IdEvento, dtpFinanceiro.Value, dtpFinanceiro.Value);
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
            btnAdicionarCusto.Enabled = true;
            VisualUtils.ExibirEstadoVazio(dgvCustos, "Nenhuma despesa registrada nesta data.");
            AtualizarConferencias(evento);
        }

        private void PrepararAbasFinanceiro()
        {
            abasFinanceiro.Name = "abasFinanceiro";
            abasFinanceiro.Dock = DockStyle.Fill;
            var abaDespesas = new TabPage("Despesas do dia");
            var abaConferencias = new TabPage("Conferências do dia");
            abaConferencias.Text = "Recebimentos por setor";
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
            abasFinanceiro.TabPages.Add(abaDespesas);
            abasFinanceiro.TabPages.Add(abaConferencias);
            abasFinanceiro.SelectedIndex = 1;
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
                if (e.RowIndex >= 0)
                    AbrirApuracaoDaJornada(Convert.ToInt32(conferencias.Rows[e.RowIndex].Tag));
            };
        }

        private void AtualizarConferencias(Evento evento)
        {
            conferencias.Rows.Clear();
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
                int linha = conferencias.Rows.Add(escala["descricao_jornada"], completo ? esperado.ToString("C2") : "Indisponível",
                    ultima == null ? "Não conferido" : ultima.ValorInformado.ToString("C2"),
                    ultima == null || !completo ? "—" : (ultima.ValorInformado - esperado).ToString("C2"), situacao,
                    ultima == null ? "—" : ultima.DataHora.ToString("dd/MM/yyyy HH:mm"));
                conferencias.Rows[linha].Cells[4].Style.ForeColor = confere ? System.Drawing.Color.SeaGreen : System.Drawing.Color.DarkOrange;
                conferencias.Rows[linha].Tag = idJornada;
            }
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
                abasFinanceiro.SelectedIndex = 0;
                labelAviso.Text = "Despesa " + id + " registrada.";
            }
            catch (Exception erro)
            {
                ExibirErro(erro);
            }
            finally
            {
                btnAdicionarCusto.Enabled = usuarioAutenticado != null;
            }
        }

        private void AbrirApuracao(object sender, EventArgs e)
        {
            AbrirApuracaoDaJornada(null);
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
                    abasFinanceiro.SelectedIndex = 1;
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

        private void panelCorpo_SizeChanged(object sender, EventArgs e)
        {
            AjustarIndicadores();
        }

        private void AjustarIndicadores()
        {
            panelCustos.SetBounds(0, 205, panelCorpo.Width, Math.Max(280, ClientSize.Height - panelCorpo.Top - 265));
            panelCorpo.Height = panelCustos.Bottom;
            labelAviso.Top = panelCorpo.Bottom + 10;
            labelAviso.Width = panelCorpo.Width;
            AutoScrollMinSize = new System.Drawing.Size(1030, labelAviso.Bottom + 16);
            Panel[] indicadores = { panelFinanceiroResumo0, panelFinanceiroResumo1, panelFinanceiroResumo2 };
            for (int i = 0; i < indicadores.Length; i++)
            {
                indicadores[i].SetBounds(i * (panelCorpo.Width + 15) / 3, 90, (panelCorpo.Width - 30) / 3, 100);
            }
        }
    }
}
