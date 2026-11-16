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

        public ApuracaoControl() : this(null, null)
        {
        }

        public ApuracaoControl(Usuario usuario, Evento evento)
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
            dtpApuracao.Value = DateTime.Today;
            numValorApurado.ReadOnly = true;
            numValorApurado.Increment = 0;
            numValorApurado.InterceptArrowKeys = false;
            numValorApurado.TabStop = false;
            numValorApurado.Maximum = 99999999.99M;
            AdicionarModalidade(dinheiro, "Dinheiro", 18);
            AdicionarModalidade(pix, "Pix", 245);
            AdicionarModalidade(cartao, "Cartão", 472);
            FrontendTheme.Aplicar(this);
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
                    cmbSetorApuracao.SelectedIndex = 0;
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
            lblSubtitulo.Text = "Vendas reais da jornada e histórico de conferências por modalidade";
            lblTotalregistradoexemplo.Text = "Total esperado atual";
            lblValorConferencia.Text = 0M.ToString("C2");
            lblValorapuradoinformadoR.Text = "Total informado nesta conferência";
            panelConferencia.Height = 225;
            panelCorpo.Height = 597;
            labelAviso.SetBounds(32, 720, 940, 62);
            labelAviso.AutoEllipsis = true;
            AutoScrollMinSize = new Size(1020, 800);
            lblSituacaoConferencia.SetBounds(18, 89, 940, 38);
            resumoAnterior.Name = "lblResumoUltimaConferencia";
            resumoAnterior.SetBounds(18, 177, 940, 43);
            resumoAnterior.AutoEllipsis = true;
            panelConferencia.Controls.Add(resumoAnterior);
            btnConferirApuracao.Text = "Registrar conferência";
            Button atualizar = new Button();
            atualizar.Name = "btnAtualizarApuracao";
            atualizar.Text = "Atualizar vendas e histórico";
            atualizar.SetBounds(730, 35, 235, 36);
            atualizar.Click += Filtros_Changed;
            panelFiltrosApuracao.Controls.Add(atualizar);

            TabControl abas = new TabControl();
            abas.Dock = DockStyle.Fill;
            TabPage vendas = new TabPage("Vendas da jornada");
            TabPage conferencias = new TabPage("Histórico de conferências");
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
"Observação" })
            {
                historico.Columns.Add(coluna, coluna);
            }
            conferencias.Controls.Add(historico);
            panelDetalheApuracao.Controls.Add(abas);
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
        }

        private void Filtros_Changed(object sender, EventArgs e)
        {

            if (carregando || salvando || usuarioAutenticado == null || eventoSelecionado == null)
            {
                return;
            }
            try
            {
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
                decimal semModalidade = 0;
                bool precosPendentes = false;
                bool modalidadePendente = false;
                dgvApuracao.Rows.Clear();
                foreach (DataRow venda in vendas.Rows)
                {
                    if (venda.IsNull("total"))
                    {
                        precosPendentes = true;
                        dgvApuracao.Rows.Add(venda["responsavel"], venda["setor"], venda["turno"],
                            Convert.ToDecimal(venda["quantidade"]).ToString("N0"), "Preço não registrado");
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
                        semModalidade += total;
                        modalidadePendente = true;
                    }
                    dgvApuracao.Rows.Add(venda["responsavel"], venda["setor"], venda["turno"],
                        Convert.ToDecimal(venda["quantidade"]).ToString("N0"), total.ToString("C2"));
                }
                lblValorConferencia.Text = (esperadoDinheiro + esperadoPix + esperadoCartao).ToString("C2");
                lblSituacaoConferencia.Text = "Esperado: Dinheiro " + esperadoDinheiro.ToString("C2") +
                    " / Pix " + esperadoPix.ToString("C2") + " / Cartão " + esperadoCartao.ToString("C2");
                labelAviso.Text = "Vendas sem modalidade: " + semModalidade.ToString("C2") +
                    ". Confira a jornada e a data antes de registrar.";
                btnConferirApuracao.Enabled = true;
                AtualizarHistorico(service.ListarPorJornada(usuarioAutenticado, jornada), !precosPendentes && !modalidadePendente);
                if (precosPendentes || modalidadePendente)
                {
                    lblValorConferencia.Text = "Indisponível";
                    lblSituacaoConferencia.Text = "O esperado está incompleto: há vendas sem preço ou modalidade válida.";
                    btnConferirApuracao.Enabled = false;
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
                    diferencaAtual, anterior.Observacao);
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
            resumoAnterior.Text = "Última conferência #" + ultima.IdApuracao + ": " +
                ultima.ValorInformado.ToString("C2") + ". Diferenças para as vendas atuais (informado − esperado): " +
                "Dinheiro " + Diferenca(ultima.ValorDinheiro, esperadoDinheiro) + "; Pix " +
                Diferenca(ultima.ValorPix, esperadoPix) + "; Cartão " + Diferenca(ultima.ValorCartao, esperadoCartao) +
                ". Uma nova conferência preserva todas as anteriores.";
        }

        private static string ValorHistorico(decimal? valor)
        {
            if (!valor.HasValue)
            {
                return "Não informado";
            }
            return valor.Value.ToString("C2");
        }

        private static string Diferenca(decimal? informado, decimal esperado)
        {
            if (!informado.HasValue)
            {
                return "não disponível";
            }
            return (informado.Value - esperado).ToString("C2");
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
                salvando = false;
                Filtros_Changed(this, EventArgs.Empty);
                btnConferirApuracao.Enabled = false;
                labelAviso.Text = "Conferência " + id + " registrada. Diferenças: Dinheiro " +
                    (dinheiro.Value - esperadoDinheiro).ToString("C2") + "; Pix " +
                    (pix.Value - esperadoPix).ToString("C2") + "; Cartão " +
                    (cartao.Value - esperadoCartao).ToString("C2") + ". Estoque e vendas preservados. Atualize para uma nova conferência.";
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
            if (erro is ArgumentException || erro is InvalidOperationException || erro is UnauthorizedAccessException)
            {
                labelAviso.Text = erro.Message;
            }
            else
            {
                labelAviso.Text = "Não foi possível carregar ou registrar a apuração.";
                System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em ApuracaoControl: " + erro.StackTrace);
            }
        }
    }
}
