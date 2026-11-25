using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE
{
    public sealed partial class CorrecaoOperacaoForm : Form
    {
        private Usuario usuario;
        private string entidade;
        private int idOperacao;
        private DataTable itens;
        private Registro registro;
        private EntradaMercadoria entrada;
        private TransferenciaEstoque transferencia;
        public CorrecaoOperacaoForm()
        {
            InitializeComponent();
        }

        public CorrecaoOperacaoForm(Usuario usuarioAutenticado, string tipoOperacao, int id)
        {
            usuario = usuarioAutenticado;
            entidade = tipoOperacao;
            idOperacao = id;
            InitializeComponent();
            Text = tipoOperacao == "registro" ? "Revisar venda ou perda" : "Correção de operação";
            IconoAplicacao.Aplicar(this);
            historico.Tag = auditoria;
            VisualUtils.ExibirEstadoVazio(auditoria, "Nenhuma correção registrada.");
            FrontendTheme.Aplicar(this);
            CarregarOperacao();
        }

        private void Grade_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            e.Cancel = true;
            mensagem.Text = "Informe números válidos nos campos de quantidade e valor.";
        }

        private void Justificativa_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(justificativa.Text))
            {
                mensagem.Text = "A confirmação atualiza os saldos e registra o motivo da correção.";
            }
        }

        private void Abas_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (abas.SelectedTab == historico)
            {
                try
                {
                    auditoria.DataSource = new ConsultaOperacionalService().HistoricoDaOperacao(usuario, entidade, idOperacao);
                }
                catch (Exception erro)
                {
                    ExibirErro(erro);
                }
            }
        }

        private void CarregarOperacao()
        {
            try
            {
                // A versão é lida antes dos itens: uma correção concorrente invalida este formulário.
                if (entidade == "registro")
                {
                    registro = new RegistroService().BuscarPorId(usuario, idOperacao);
                }
                else if (entidade == "entradamercadoria")
                {
                    entrada = new EntradaMercadoriaService().BuscarPorId(usuario, idOperacao);
                }
                else if (entidade == "transferenciaestoque")
                {
                    transferencia = new TransferenciaEstoqueService().BuscarPorId(usuario, idOperacao);
                }
                else
                {
                    throw new ArgumentException("Tipo de operação inválido.");
                }

                itens = new ConsultaOperacionalService().ItensParaCorrecao(usuario, entidade, idOperacao);
                grade.DataSource = itens;
                grade.Columns["id_item"].Visible = false;
                grade.Columns["id_produto"].Visible = false;
                grade.Columns["produto"].ReadOnly = true;
                grade.Columns["produto"].HeaderText = "Produto";
                grade.Columns["quantidade"].HeaderText = "Quantidade";
                grade.Columns["quantidade"].DefaultCellStyle.Format = "N0";
                grade.Columns["valor_unitario"].HeaderText = "Valor unitário (R$)";
                grade.Columns["valor_unitario"].DefaultCellStyle.Format = "N2";
                if (entidade == "registro")
                {
                    PrepararProdutosDoRegistro();
                    grade.Columns["valor_unitario"].HeaderText = "Preço praticado (R$)";
                    tipo.Items.AddRange(new object[] { "venda", "perda" });
                    tipo.SelectedItem = registro.Tipo;
                    pagamento.SelectedItem = registro.FormaPagamento;
                    observacao.Text = registro.Observacao;
                    contexto.Text = (registro.Tipo == "venda" ? "Venda" : "Perda") + " • Registro " + idOperacao + " • " + registro.DataHora.ToString("g") + "\nCorreção do lançamento original e histórico de alterações.";
                    tipo.SelectedIndexChanged += tipo_SelectedIndexChanged;
                    AtualizarTipoRegistro();
                }
                else if (entidade == "entradamercadoria")
                {
                    observacao.Text = entrada.Observacao;
                    tipo.Visible = pagamento.Visible = false;
                    contexto.Text = "Entrada " + idOperacao + " · " + entrada.DataHora.ToString("g") + " · versão " + entrada.Versao;
                }
                else
                {
                    observacao.Text = transferencia.Observacao;
                    tipo.Visible = pagamento.Visible = false;
                    grade.Columns["valor_unitario"].Visible = false;
                    grade.Columns["quantidade"].ReadOnly = transferencia.IdSolicitacao.HasValue || transferencia.IdSolicitacaoEstoqueCentral.HasValue;
                    contexto.Text = "Transferência " + idOperacao + " · " + transferencia.DataHora.ToString("g") + " · versão " + transferencia.Versao;
                    if (grade.Columns["quantidade"].ReadOnly)
                    {
                        contexto.Text += ". Entrega integral: somente observação pode ser corrigida.";
                    }
                }

                mensagem.Text = "Datas, locais, autores e itens originais serão preservados. A alteração será auditada.";
                if (registro != null)
                {
                    mensagem.Text = "Produto correto: selecione na lista do mesmo setor e revise o preço praticado. Justificativa obrigatória; saldos e histórico serão atualizados juntos.";
                }
            }
            catch (Exception erro)
            {
                salvar.Enabled = false;
                ExibirErro(erro);
            }
        }

        private void PrepararProdutosDoRegistro()
        {
            DataTable produtos = new ConsultaOperacionalService().ProdutosDaEscala(usuario, registro.IdEscala);
            foreach (DataRow item in itens.Rows)
            {
                int id = Convert.ToInt32(item["id_produto"]);
                bool encontrado = false;
                foreach (DataRow produto in produtos.Rows)
                {
                    if (Convert.ToInt32(produto["id_produto_setor"]) == id)
                    {
                        encontrado = true;
                        break;
                    }
                }

                // Um produto histórico inativo continua visível; o backend só permite mantê-lo na própria linha.
                if (!encontrado)
                {
                    produtos.Rows.Add(id, item["produto"], DBNull.Value);
                }
            }

            grade.Columns["produto"].Visible = false;
            DataGridViewComboBoxColumn selecao = new DataGridViewComboBoxColumn();
            selecao.Name = "produtoCorrecao";
            selecao.HeaderText = "Produto do setor";
            selecao.DataPropertyName = "id_produto";
            selecao.DataSource = produtos;
            selecao.DisplayMember = "nome";
            selecao.ValueMember = "id_produto_setor";
            selecao.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            selecao.FlatStyle = FlatStyle.Standard;
            grade.Columns.Add(selecao);
            selecao.DisplayIndex = 0;
        }

        private void AtualizarTipoRegistro()
        {
            bool venda = tipo.Text == "venda";
            pagamento.Enabled = venda;
            grade.Columns["valor_unitario"].ReadOnly = !venda;
        }

        private void SalvarCorrecao(object sender, EventArgs e)
        {
            if (!grade.EndEdit())
            {
                mensagem.Text = "Corrija o valor digitado antes de confirmar.";
                return;
            }

            salvar.Enabled = false;
            try
            {
                if (registro != null)
                {
                    List<RegistroItem> lista = new List<RegistroItem>();
                    foreach (DataRow linha in itens.Rows)
                    {
                        lista.Add(new RegistroItem { IdRegistroItem = Convert.ToInt32(linha["id_item"]), IdRegistro = idOperacao, IdProdutoSetor = Convert.ToInt32(linha["id_produto"]), Quantidade = Convert.ToDecimal(linha["quantidade"]), PrecoUnitarioPraticado = ValorOpcional(linha) });
                    }

                    registro.Tipo = tipo.Text;
                    registro.FormaPagamento = pagamento.Text;
                    registro.Observacao = observacao.Text;
                    new RegistroService().Editar(usuario, registro, lista, justificativa.Text);
                }
                else if (entrada != null)
                {
                    List<EntradaItem> lista = new List<EntradaItem>();
                    foreach (DataRow linha in itens.Rows)
                    {
                        lista.Add(new EntradaItem { IdEntradaItem = Convert.ToInt32(linha["id_item"]), IdEntrada = idOperacao, IdProduto = Convert.ToInt32(linha["id_produto"]), Quantidade = Convert.ToDecimal(linha["quantidade"]), CustoUnitario = ValorOpcional(linha) });
                    }

                    entrada.Observacao = observacao.Text;
                    new EntradaMercadoriaService().Editar(usuario, entrada, lista, justificativa.Text);
                }
                else if (transferencia != null)
                {
                    List<TransferenciaItem> lista = new List<TransferenciaItem>();
                    foreach (DataRow linha in itens.Rows)
                    {
                        lista.Add(new TransferenciaItem { IdTransferenciaItem = Convert.ToInt32(linha["id_item"]), IdTransferencia = idOperacao, IdProduto = Convert.ToInt32(linha["id_produto"]), Quantidade = Convert.ToDecimal(linha["quantidade"]) });
                    }

                    transferencia.Observacao = observacao.Text;
                    new TransferenciaEstoqueService().Editar(usuario, transferencia, lista, justificativa.Text);
                }
                else
                {
                    throw new InvalidOperationException("Reabra uma operação válida antes de corrigir.");
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception erro)
            {
                salvar.Enabled = true;
                ExibirErro(erro);
            }
        }

        private static decimal? ValorOpcional(DataRow linha)
        {
            if (linha.IsNull("valor_unitario"))
            {
                return null;
            }

            return Convert.ToDecimal(linha["valor_unitario"]);
        }

        private void ExibirErro(Exception erro)
        {
            FeedbackUsuario.ExibirErro(this, erro, "Não foi possível consultar ou corrigir a operação. Confira os dados e tente novamente.", mensagem);
            Trace.WriteLine(erro.GetType().Name + " em CorrecaoOperacaoForm: " + erro.StackTrace);
        }

        private void tipo_SelectedIndexChanged(object s, EventArgs e)
        {
            AtualizarTipoRegistro();
        }
    }
}
