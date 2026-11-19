using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE.Quermesse
{
    public partial class ApuracaoControl : UserControl
    {
        private readonly Usuario usuarioAutenticado;
        private readonly Evento eventoSelecionado;
        private readonly NumericUpDown dinheiro = new NumericUpDown();
        private readonly NumericUpDown pix = new NumericUpDown();
        private readonly NumericUpDown cartao = new NumericUpDown();
        private bool carregando = true;
        private decimal esperadoDinheiro;
        private decimal esperadoPix;
        private decimal esperadoCartao;
        private int ultimaConferencia;
        private readonly DataGridView historico = new DataGridView();
        private readonly Label resumoAnterior = new Label();
        private bool salvando;
        private int jornadaCarregada;
        private readonly DataGridView comparacao = new DataGridView();
        private readonly TabControl abas = new TabControl();
        private readonly Label totalRecebido = new Label();
        private bool podeConferir;
        public DateTime? DataConferida { get; private set; }

        public void SelecionarJornada(int idJornada)
        {
            foreach (DataRowView jornada in cmbSetorApuracao.Items)
                if (Convert.ToInt32(jornada["id_jornada"]) == idJornada)
                {
                    cmbSetorApuracao.SelectedItem = jornada;
                    return;
                }
        }

        public ApuracaoControl() : this(null, null)
        {
        }

        public ApuracaoControl(Usuario usuario, Evento evento) : this(usuario, evento, null)
        {
        }

        public ApuracaoControl(Usuario usuario, Evento evento, DateTime? dataInicial)
        {
            InitializeComponent();
            usuarioAutenticado = usuario;
            eventoSelecionado = evento;
            FrontendTheme.Aplicar(this);
            PrepararConferencia();
            cmbSetorApuracao.Items.Clear();
            cmbTurnoApuracao.Items.Clear();
            cmbTurnoApuracao.Visible = false;
            lblTurno.Visible = false;
            lblSetor.Text = "Jornada / setor / turno";
            cmbSetorApuracao.Width = 490;
            cmbSetorApuracao.DisplayMember = "descricao_jornada";
            dtpApuracao.Value = dataInicial ?? DateTime.Today;
            dtpApuracao.Enabled = false;
            numValorApurado.ReadOnly = true;
            numValorApurado.Increment = 0;
            numValorApurado.InterceptArrowKeys = false;
            numValorApurado.TabStop = false;
            numValorApurado.Visible = false;
            numValorApurado.Maximum = 99999999.99M;
            AdicionarModalidade(dinheiro, "Dinheiro", 18);
            AdicionarModalidade(pix, "Pix", 245);
            AdicionarModalidade(cartao, "Cartão", 472);
            FrontendTheme.Aplicar(this);
            comparacao.RowTemplate.Height = 26;
            comparacao.ColumnHeadersHeight = 32;
            comparacao.EnableHeadersVisualStyles = false;
            VisualUtils.ExibirEstadoVazio(dgvApuracao, "Nenhuma venda registrada nesta jornada.");
            VisualUtils.ExibirEstadoVazio(historico, "Nenhuma conferência registrada nesta jornada.");
            btnConferirApuracao.Enabled = false;
            lblSituacaoConferencia.Text = "Selecione uma jornada autorizada.";
            labelAviso.Text = "Conferências não alteram vendas nem estoque.";
            if (usuario == null || evento == null)
            {
                lblValorConferencia.Text = 0M.ToString("C2");
                labelAviso.Text = "Selecione um evento com uma sessão autorizada para consultar a apuração.";
                carregando = false;
                return;
            }
            try
            {
                DataTable escalas = new ConsultaOperacionalService().Escalas(usuario, evento.IdEvento);
                System.Collections.Generic.HashSet<int> jornadas = new System.Collections.Generic.HashSet<int>();
                foreach (DataRowView escala in escalas.DefaultView)
                {
                    if (jornadas.Add(Convert.ToInt32(escala["id_jornada"])))
                    {
                        cmbSetorApuracao.Items.Add(escala);
                    }
                }
                carregando = false;
                if (cmbSetorApuracao.Items.Count > 0)
                {
                    if (!dataInicial.HasValue)
                        cmbSetorApuracao.SelectedIndex = 0;
                    else
                    {
                        foreach (DataRowView jornada in cmbSetorApuracao.Items)
                            if (Convert.ToDateTime(jornada["data_jornada"]).Date == dataInicial.Value.Date)
                            {
                                cmbSetorApuracao.SelectedItem = jornada;
                                break;
                            }
                        if (cmbSetorApuracao.SelectedIndex < 0)
                            labelAviso.Text = "Não há jornada com escala em " + dataInicial.Value.ToString("dd/MM/yyyy") +
                                ". Selecione outra jornada na lista para conferir.";
                    }
                }
                else
                {
                    labelAviso.Text = "Não há jornadas com escalas neste evento. Cadastre a equipe, a jornada e as escalas antes de conferir.";
                }
            }
            catch (Exception erro)
            {
                ExibirErro(erro);
            }
            finally
            {
                carregando = false;
            }
        }

        private void PrepararConferencia()
        {
            lblTitulo.Text = "Conferência de recebimentos";
            lblSubtitulo.Text = "Compare as vendas com o que foi recebido em dinheiro, Pix e cartão. Cada conferência fica no histórico.";
            lblTotalregistradoexemplo.Text = "Vendas esperadas na jornada";
            lblValorConferencia.Text = 0M.ToString("C2");
            lblValorapuradoinformadoR.Text = "Total recebido (soma automática)";
            totalRecebido.Name = "lblTotalRecebido";
            totalRecebido.SetBounds(325, 44, 300, 45);
            totalRecebido.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            totalRecebido.ForeColor = VisualUtils.Marinho;
            totalRecebido.Text = 0M.ToString("C2");
            panelConferencia.Controls.Add(totalRecebido);
            panelDetalheApuracao.Height = 430;
            panelConferencia.Top = 0;
            panelConferencia.Height = 350;
            panelCorpo.Height = 535;
            labelAviso.SetBounds(32, 660, 980, 48);
            labelAviso.AutoEllipsis = true;
            AutoScrollMinSize = new Size(1020, 720);
            lblSituacaoConferencia.SetBounds(18, 89, 940, 38);
            resumoAnterior.Name = "lblResumoUltimaConferencia";
            resumoAnterior.SetBounds(18, 303, 940, 40);
            resumoAnterior.AutoEllipsis = true;
            panelConferencia.Controls.Add(resumoAnterior);
            btnConferirApuracao.Text = "Registrar conferência";
            Button atualizar = new Button();
            atualizar.Name = "btnAtualizarApuracao";
            atualizar.Text = "Atualizar vendas e histórico";
            atualizar.SetBounds(730, 35, 235, 36);
            atualizar.Click += Filtros_Changed;
            panelFiltrosApuracao.Controls.Add(atualizar);

            abas.Name = "abasApuracao";
            abas.Dock = DockStyle.Fill;
            TabPage vendas = new TabPage("Vendas da jornada") { BackColor = VisualUtils.Fundo };
            TabPage conferencias = new TabPage("Histórico de conferências") { BackColor = VisualUtils.Fundo };
            TabPage conferir = new TabPage("Conferir recebimentos") { BackColor = VisualUtils.Fundo };
            panelCorpo.Controls.Remove(panelConferencia);
            panelConferencia.Dock = DockStyle.Top;
            conferir.Controls.Add(panelConferencia);
            abas.TabPages.Add(conferir);
            abas.TabPages.Add(vendas);
            abas.TabPages.Add(conferencias);
            panelDetalheApuracao.Controls.Remove(dgvApuracao);
            dgvApuracao.Dock = DockStyle.Fill;
            vendas.Controls.Add(dgvApuracao);
            historico.Name = "dgvHistoricoApuracao";
            historico.Dock = DockStyle.Fill;
            historico.ReadOnly = true;
            historico.AllowUserToAddRows = false;
            historico.AllowUserToDeleteRows = false;
            historico.RowHeadersVisible = false;
            historico.EnableHeadersVisualStyles = false;
            historico.BackgroundColor = Color.White;
            historico.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            foreach (string coluna in new[] { "Conferência",
"Data/hora",
"Responsável (ID)",
"Dinheiro",
"Pix",
"Cartão",
"Total informado",
"Diferença atual",
"Situação atual",
"Observação" })
            {
                historico.Columns.Add(coluna, coluna);
            }
            conferencias.Controls.Add(historico);
            panelDetalheApuracao.Controls.Add(abas);
            dgvApuracao.Columns.Add("formaPagamento", "Pagamento");
            comparacao.Name = "dgvComparacaoConferencia";
            comparacao.SetBounds(18, 177, 947, 116);
            comparacao.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            comparacao.ReadOnly = true;
            comparacao.AllowUserToAddRows = false;
            comparacao.AllowUserToDeleteRows = false;
            comparacao.AllowUserToResizeRows = false;
            comparacao.RowHeadersVisible = false;
            comparacao.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            comparacao.BackgroundColor = Color.White;
            comparacao.RowTemplate.Height = 26;
            foreach (string coluna in new[] { "Modalidade", "Vendas esperadas", "Recebido informado", "Diferença" })
                comparacao.Columns.Add(coluna, coluna);
            panelConferencia.Controls.Add(comparacao);
            panelConferencia.SizeChanged += (s, e) =>
                comparacao.Width = Math.Max(700, panelConferencia.ClientSize.Width - 36);
        }

        private void AdicionarModalidade(NumericUpDown campo, string titulo, int esquerda)
        {
            campo.DecimalPlaces = 2;
            campo.Maximum = 99999999.99M;
            campo.Name = "numConferencia" + titulo.Replace("ã", "a");
            campo.SetBounds(esquerda + 75, 137, 140, 28);
            panelConferencia.Controls.Add(FrontendTheme.Legenda(titulo,
                new Rectangle(esquerda, 140, 75, 24)));
            panelConferencia.Controls.Add(campo);
            campo.ValueChanged += AtualizarTotal;
        }

        private void AtualizarTotal(object sender, EventArgs e)
        {
            decimal total = dinheiro.Value + pix.Value + cartao.Value;
            numValorApurado.Value = Math.Min(total, numValorApurado.Maximum);
            totalRecebido.Text = total.ToString("C2");
            comparacao.Rows.Clear();
            decimal[] esperados = { esperadoDinheiro, esperadoPix, esperadoCartao };
            decimal[] informados = { dinheiro.Value, pix.Value, cartao.Value };
            string[] nomes = { "Dinheiro", "Pix", "Cartão" };
            for (int i = 0; i < nomes.Length; i++)
            {
                int linha = comparacao.Rows.Add(nomes[i], esperados[i].ToString("C2"), informados[i].ToString("C2"),
                    (informados[i] - esperados[i]).ToString("C2"));
                comparacao.Rows[linha].Cells[3].Style.ForeColor = informados[i] == esperados[i] ? Color.SeaGreen : Color.Firebrick;
            }
            if (!carregando && !salvando && podeConferir)
            {
                btnConferirApuracao.Enabled = true;
                btnConferirApuracao.Text = ultimaConferencia == 0 ? "Registrar conferência" : "Registrar nova conferência";
            }
        }

        private void Filtros_Changed(object sender, EventArgs e)
        {

            if (carregando || salvando || usuarioAutenticado == null || eventoSelecionado == null)
            {
                return;
            }
            try
            {
                podeConferir = false;
                btnConferirApuracao.Enabled = false;
                dgvApuracao.Rows.Clear();
                historico.Rows.Clear();
                resumoAnterior.Text = "";
                lblValorConferencia.Text = "—";
                lblSituacaoConferencia.Text = "Consultando a jornada selecionada...";
                DataRowView escala = cmbSetorApuracao.SelectedItem as DataRowView;
                if (escala == null)
                {
                    lblSituacaoConferencia.Text = "Selecione uma jornada autorizada.";
                    return;
                }
                int jornada = Convert.ToInt32(escala["id_jornada"]);
                if (jornadaCarregada != jornada)
                {
                    dinheiro.Value = 0;
                    pix.Value = 0;
                    cartao.Value = 0;
                    jornadaCarregada = jornada;
                }
                carregando = true;
                dtpApuracao.Value = Convert.ToDateTime(escala["data_jornada"]);
                dtpApuracao.Enabled = false;
                carregando = false;
                ApuracaoService service = new ApuracaoService();
                DataTable vendas = service.VendasPorJornada(usuarioAutenticado, jornada);
                esperadoDinheiro = 0;
                esperadoPix = 0;
                esperadoCartao = 0;
                bool precosPendentes = false;
                bool modalidadePendente = false;
                dgvApuracao.Rows.Clear();
                foreach (DataRow venda in vendas.Rows)
                {
                    if (venda.IsNull("total"))
                    {
                        precosPendentes = true;
                        dgvApuracao.Rows.Add(venda["responsavel"], venda["setor"], venda["turno"],
                            Convert.ToDecimal(venda["quantidade"]).ToString("N0"), "Preço não registrado", venda["forma_pagamento"]);
                        continue;
                    }
                    decimal total = Convert.ToDecimal(venda["total"]);
                    string modalidade = Convert.ToString(venda["forma_pagamento"]).Trim().ToLowerInvariant();
                    if (modalidade == "dinheiro")
                    {
                        esperadoDinheiro += total;
                    }
                    else if (modalidade == "pix")
                    {
                        esperadoPix += total;
                    }
                    else if (modalidade == "cartao")
                    {
                        esperadoCartao += total;
                    }
                    else
                    {
                        modalidadePendente = true;
                    }
                    dgvApuracao.Rows.Add(venda["responsavel"], venda["setor"], venda["turno"],
                        Convert.ToDecimal(venda["quantidade"]).ToString("N0"), total.ToString("C2"),
                        modalidade == "cartao" ? "Cartão" : modalidade == "pix" ? "Pix" : modalidade == "dinheiro" ? "Dinheiro" : "Não informado");
                }
                lblValorConferencia.Text = (esperadoDinheiro + esperadoPix + esperadoCartao).ToString("C2");
                lblSituacaoConferencia.Text = "Informe abaixo os valores realmente recebidos. Diferença negativa indica falta; positiva indica sobra.";
                labelAviso.Text = "A conferência não altera vendas, despesas nem estoque. O histórico compara cada conferência com as vendas atuais.";
                btnConferirApuracao.Enabled = true;
                AtualizarHistorico(service.ListarPorJornada(usuarioAutenticado, jornada), !precosPendentes && !modalidadePendente);
                podeConferir = !precosPendentes && !modalidadePendente;
                AtualizarTotal(this, EventArgs.Empty);
                if (precosPendentes || modalidadePendente)
                {
                    lblValorConferencia.Text = "Indisponível";
                    lblSituacaoConferencia.Text = "O esperado está incompleto: há vendas sem preço ou modalidade válida.";
                    btnConferirApuracao.Enabled = false;
                    comparacao.Rows.Clear();
                    labelAviso.Text = "Há vendas sem preço ou modalidade válida. Regularize os registros com correção auditável antes de conferir.";
                }
            }
            catch (Exception erro)
            {
                btnConferirApuracao.Enabled = false;
                ExibirErro(erro);
            }
        }

        private void AtualizarHistorico(System.Collections.Generic.List<Apuracao> anteriores, bool valoresCompletos)
        {
            historico.Rows.Clear();
            ultimaConferencia = 0;
            Apuracao ultima = null;
            decimal esperado = esperadoDinheiro + esperadoPix + esperadoCartao;
            foreach (Apuracao anterior in anteriores)
            {
                string diferencaAtual = "Indisponível";
                if (valoresCompletos)
                {
                    diferencaAtual = (anterior.ValorInformado - esperado).ToString("C2");
                }
                historico.Rows.Add(anterior.IdApuracao, anterior.DataHora, anterior.IdUsuarioResponsavel,
                    ValorHistorico(anterior.ValorDinheiro), ValorHistorico(anterior.ValorPix),
                    ValorHistorico(anterior.ValorCartao), anterior.ValorInformado.ToString("C2"),
                    diferencaAtual, !valoresCompletos ? "Vendas incompletas" :
                        !anterior.ValorDinheiro.HasValue || !anterior.ValorPix.HasValue || !anterior.ValorCartao.HasValue ? "Histórico sem modalidades" :
                        anterior.ValorDinheiro == esperadoDinheiro && anterior.ValorPix == esperadoPix && anterior.ValorCartao == esperadoCartao
                            ? "Valores conferem" : "Com divergência", anterior.Observacao);
                if (anterior.IdApuracao > ultimaConferencia)
                {
                    ultimaConferencia = anterior.IdApuracao;
                    ultima = anterior;
                }
            }
            if (ultima == null)
            {
                resumoAnterior.Text = "Nenhuma conferência anterior. Informe os valores conferidos por modalidade.";
                return;
            }
            if (!valoresCompletos)
            {
                resumoAnterior.Text = "Última conferência #" + ultima.IdApuracao + ": " +
                    ultima.ValorInformado.ToString("C2") + ". Valores históricos preservados; diferenças indisponíveis até regularizar as vendas.";
                return;
            }
            bool detalhada = ultima.ValorDinheiro.HasValue && ultima.ValorPix.HasValue && ultima.ValorCartao.HasValue;
            bool confere = detalhada && ultima.ValorDinheiro == esperadoDinheiro && ultima.ValorPix == esperadoPix && ultima.ValorCartao == esperadoCartao;
            resumoAnterior.ForeColor = confere ? Color.SeaGreen : Color.DarkOrange;
            resumoAnterior.Text = "Última conferência: " + ultima.DataHora.ToString("dd/MM/yyyy HH:mm") + " • " +
                ultima.ValorInformado.ToString("C2") + " • " + (!detalhada ? "Histórico sem modalidades" : confere ? "Valores conferem" : "Com divergência") +
                ". Consulte os registros na aba Histórico de conferências.";
        }

        private static string ValorHistorico(decimal? valor)
        {
            if (!valor.HasValue)
            {
                return "Não informado";
            }
            return valor.Value.ToString("C2");
        }

        private void btnConferirApuracao_Click(object sender, EventArgs e)
        {
            if (salvando || !btnConferirApuracao.Enabled)
            {
                return;
            }
            DataRowView escala = cmbSetorApuracao.SelectedItem as DataRowView;
            if (escala == null)
            {
                return;
            }
            if (dtpApuracao.Value.Date != Convert.ToDateTime(escala["data_jornada"]).Date)
            {
                labelAviso.Text = "Selecione a data correspondente à jornada antes de conferir.";
                return;
            }
            btnConferirApuracao.Enabled = false;
            salvando = true;
            try
            {
                Apuracao apuracao = new Apuracao();
                apuracao.IdJornada = Convert.ToInt32(escala["id_jornada"]);
                apuracao.ValorDinheiro = dinheiro.Value;
                apuracao.ValorPix = pix.Value;
                apuracao.ValorCartao = cartao.Value;
                apuracao.Observacao = "Conferência por modalidade; data selecionada: " +
                    dtpApuracao.Value.ToString("yyyy-MM-dd");
                int id = new ApuracaoService().RegistrarConferencia(usuarioAutenticado, apuracao, ultimaConferencia);
                DataConferida = dtpApuracao.Value.Date;
                salvando = false;
                Filtros_Changed(this, EventArgs.Empty);
                btnConferirApuracao.Enabled = false;
                btnConferirApuracao.Text = "Conferência salva";
                abas.SelectedIndex = 2;
                if (historico.Rows.Count > 0)
                {
                    historico.ClearSelection();
                    historico.CurrentCell = historico.Rows[historico.Rows.Count - 1].Cells[0];
                }
                labelAviso.ForeColor = Color.SeaGreen;
                labelAviso.Text = "Conferência " + id + " salva no histórico. Diferenças: Dinheiro " +
                    (dinheiro.Value - esperadoDinheiro).ToString("C2") + "; Pix " +
                    (pix.Value - esperadoPix).ToString("C2") + "; Cartão " +
                    (cartao.Value - esperadoCartao).ToString("C2") + ". Ao fechar, veja o resumo em Conferências do dia.";
            }
            catch (Exception erro)
            {
                ExibirErro(erro);
                btnConferirApuracao.Enabled = true;
            }
            finally
            {
                salvando = false;
            }
        }

        private void ExibirErro(Exception erro)
        {
            labelAviso.ForeColor = Color.Firebrick;
            CASE.Utils.FeedbackUsuario.ExibirErro(this, erro,
                "Não foi possível carregar ou registrar a apuração. Confira os valores e tente novamente.", labelAviso);
            System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em ApuracaoControl: " + erro.StackTrace);
        }
    }
}
