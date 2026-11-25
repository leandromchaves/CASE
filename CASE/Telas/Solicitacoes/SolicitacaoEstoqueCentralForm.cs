using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE.Quermesse
{
    public partial class SolicitacaoEstoqueCentralForm : Form
    {
        private Usuario usuarioAutenticado;
        private Evento eventoSelecionado;
        private SolicitacaoEstoqueCentralService solicitacaoService =
            new SolicitacaoEstoqueCentralService();
        private ProdutoService produtoService = new ProdutoService();
        private List<Produto> produtos = new List<Produto>();

        public SolicitacaoEstoqueCentralForm()
        {
            InitializeComponent();
        }

        public SolicitacaoEstoqueCentralForm(Usuario usuario, Evento evento) : this()
        {
            usuarioAutenticado = usuario;
            eventoSelecionado = evento;
            CarregarProdutos();
            dgvItens.BackgroundColor = Color.White;
            dgvItens.EnableHeadersVisualStyles = false;
            FrontendTheme.Aplicar(this);
            IconoAplicacao.Aplicar(this);
        }

        private void dgvItens_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            lblAviso.Text = "Selecione um produto válido na lista e revise a quantidade.";
        }

        private void CarregarProdutos()
        {
            try
            {
                List<Produto> todosProdutos = produtoService.Listar(usuarioAutenticado);

                foreach (Produto produto in todosProdutos)
                {
                    if (produto.Ativo)
                    {
                        produtos.Add(produto);
                    }
                }

                DataGridViewComboBoxColumn colunaProduto =
                    (DataGridViewComboBoxColumn)dgvItens.Columns["produto"];
                colunaProduto.DataSource = produtos;
                colunaProduto.DisplayMember = "Nome";
                colunaProduto.ValueMember = "IdProduto";

                if (produtos.Count == 0)
                {
                    btnAdicionarItem.Enabled = false;
                    btnEnviar.Enabled = false;
                    lblAviso.Text = "Não há produtos ativos para solicitar.";
                    return;
                }

                AdicionarLinha();
            }
            catch (Exception)
            {
                btnAdicionarItem.Enabled = false;
                btnEnviar.Enabled = false;
                lblAviso.Text = "Não foi possível carregar os produtos.";
            }
        }

        private void btnAdicionarItem_Click(object sender, EventArgs e)
        {
            AdicionarLinha();
        }

        private void AdicionarLinha()
        {
            if (produtos.Count == 0)
            {
                return;
            }

            int primeiraLinha = dgvItens.Rows.Add(produtos[0].IdProduto, "1");
            dgvItens.CurrentCell = dgvItens.Rows[primeiraLinha].Cells[0];
        }

        private void btnRemoverItem_Click(object sender, EventArgs e)
        {
            if (dgvItens.SelectedRows.Count > 0)
            {
                dgvItens.Rows.Remove(dgvItens.SelectedRows[0]);
            }
        }

        private void btnEnviar_Click(object sender, EventArgs e)
        {
            try
            {
                dgvItens.EndEdit();
                List<SolicitacaoEstoqueCentralItem> itens = MontarItens();
                int idSolicitacao;

                if (cmbTipo.SelectedIndex == 0)
                {
                    idSolicitacao = solicitacaoService.CriarLiberacao(
                        usuarioAutenticado,
                        eventoSelecionado.IdEvento,
                        itens);
                }
                else
                {
                    idSolicitacao = solicitacaoService.CriarDevolucao(
                        usuarioAutenticado,
                        eventoSelecionado.IdEvento,
                        itens);
                }

                MessageBox.Show(
                    "Pedido " + idSolicitacao + " enviado para análise administrativa.",
                    "Solicitação enviada",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (ArgumentException ex)
            {
                FeedbackUsuario.ExibirErro(this, ex, "Confira os dados da solicitação e tente novamente.", lblAviso);
            }
            catch (InvalidOperationException ex)
            {
                FeedbackUsuario.ExibirErro(this, ex, "Confira os dados da solicitação e tente novamente.", lblAviso);
            }
            catch (UnauthorizedAccessException)
            {
                lblAviso.Text = "Sua conta não pode enviar este pedido.";
            }
            catch (Exception)
            {
                lblAviso.Text = "Não foi possível registrar a solicitação.";
            }
        }

        private List<SolicitacaoEstoqueCentralItem> MontarItens()
        {
            List<SolicitacaoEstoqueCentralItem> itens =
                new List<SolicitacaoEstoqueCentralItem>();

            foreach (DataGridViewRow linha in dgvItens.Rows)
            {
                if (linha.IsNewRow)
                {
                    continue;
                }

                object valorProduto = linha.Cells[0].Value;
                int idProduto;
                int quantidade;

                if (valorProduto == null ||
                    !int.TryParse(valorProduto.ToString(), out idProduto) ||
                    !int.TryParse(Convert.ToString(linha.Cells[1].Value), out quantidade) ||
                    quantidade <= 0)
                {
                    throw new ArgumentException(
                        "Selecione um produto e informe uma quantidade inteira maior que zero.");
                }

                SolicitacaoEstoqueCentralItem item = new SolicitacaoEstoqueCentralItem();
                item.IdProduto = idProduto;
                item.Quantidade = quantidade;
                itens.Add(item);
            }

            if (itens.Count == 0)
            {
                throw new ArgumentException("Adicione pelo menos um produto ao pedido.");
            }

            return itens;
        }
    }
}
