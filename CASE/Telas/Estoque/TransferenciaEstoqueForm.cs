using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE
{
    public partial class TransferenciaEstoqueForm : Form
    {
        private readonly Usuario usuarioAutenticado;
        private readonly List<LocalEstoque> locaisEvento = new List<LocalEstoque>();
        private readonly bool transferenciaCentral;
        private readonly int? idLocalOrigemSolicitado;
        private readonly TransferenciaEstoqueService transferenciaService =
            new TransferenciaEstoqueService();
        private readonly LocalEstoqueService localEstoqueService = new LocalEstoqueService();
        private readonly EstoqueService estoqueService = new EstoqueService();
        private readonly ProdutoService produtoService = new ProdutoService();
        private readonly string chaveOperacao = Guid.NewGuid().ToString("N");
        private bool salvando;

        public TransferenciaEstoqueForm() : this(null)
        {
        }

        public TransferenciaEstoqueForm(Usuario usuario) : this(usuario, null)
        {
        }

        public TransferenciaEstoqueForm(Usuario usuario, int? idLocalOrigem)
            : this(usuario, idLocalOrigem, false)
        {
        }

        public TransferenciaEstoqueForm(
            Usuario usuario,
            int? idLocalOrigem,
            bool transferenciaCentral)
        {
            InitializeComponent();
            IconoAplicacao.Aplicar(this);
            usuarioAutenticado = usuario;
            this.transferenciaCentral = transferenciaCentral;
            idLocalOrigemSolicitado = idLocalOrigem;
            lblModoApresentacao.Visible = false;

            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime)
            {
                return;
            }

            cmbOrigem.DisplayMember = "Descricao";
            cmbDestino.DisplayMember = "Descricao";
            cmbProduto.DisplayMember = "Nome";
            if (transferenciaCentral)
            {
                Text = "Transferência do estoque central";
                lblTitulo.Text = "Transferir para evento";
                lblSubtitulo.Text = "Envie produtos do estoque central diretamente ao estoque geral do evento.";
                lblOrigem.Text = "Estoque central";
                lblDestino.Text = "Estoque geral do evento";
                btnConfirmar.Text = "Confirmar transferência";
                cmbOrigem.Enabled = false;
            }
            numQuantidade.DecimalPlaces = 0;
            numQuantidade.Minimum = 1;
            cmbOrigem.SelectedIndexChanged += cmbOrigem_SelectedIndexChanged;
            cmbProduto.SelectedIndexChanged += cmbProduto_SelectedIndexChanged;
            btnConfirmar.Click += btnConfirmar_Click;
            FrontendTheme.Aplicar(this);

            if (usuarioAutenticado == null)
            {
                btnConfirmar.Enabled = false;
                lblMensagem.Text = "Autentique-se para transferir produtos.";
                return;
            }

            CarregarDados();
            if (idLocalOrigem.HasValue)
                foreach (LocalEstoque local in cmbOrigem.Items)
                    if (local.IdLocalEstoque == idLocalOrigem.Value)
                    {
                        cmbOrigem.SelectedItem = local;
                        break;
                    }
        }

        private void CarregarDados()
        {
            try
            {
                List<LocalEstoque> locaisAutorizados = localEstoqueService.Listar(usuarioAutenticado);
                LocalEstoque centralSelecionado = null;

                foreach (LocalEstoque local in locaisAutorizados)
                {
                    if (!local.Ativo)
                    {
                        continue;
                    }

                    if (transferenciaCentral && local.EhCentral &&
                        (!idLocalOrigemSolicitado.HasValue ||
                         local.IdLocalEstoque == idLocalOrigemSolicitado.Value))
                    {
                        centralSelecionado = local;
                    }
                    else if (!local.EhCentral && local.IdEvento.HasValue)
                    {
                        locaisEvento.Add(local);
                    }
                }

                if (transferenciaCentral)
                {
                    if (centralSelecionado == null)
                    {
                        btnConfirmar.Enabled = false;
                        lblMensagem.Text = "O estoque central selecionado não está disponível para sua conta.";
                        return;
                    }

                    cmbOrigem.Items.Add(centralSelecionado);
                    foreach (LocalEstoque local in locaisEvento)
                    {
                        if (local.EhGeralEvento &&
                            local.IdInstituicao == centralSelecionado.IdInstituicao)
                        {
                            cmbDestino.Items.Add(local);
                        }
                    }
                }
                else
                {
                    foreach (LocalEstoque local in locaisEvento)
                    {
                        cmbOrigem.Items.Add(local);
                    }
                }

                foreach (Produto produto in produtoService.Listar(usuarioAutenticado))
                {
                    if (produto.Ativo)
                    {
                        cmbProduto.Items.Add(produto);
                    }
                }

                if (cmbOrigem.Items.Count > 0)
                {
                    cmbOrigem.SelectedIndex = 0;
                }

                if (cmbProduto.Items.Count > 0)
                {
                    cmbProduto.SelectedIndex = 0;
                }

                if (cmbOrigem.Items.Count == 0 || cmbProduto.Items.Count == 0 ||
                    (transferenciaCentral && cmbDestino.Items.Count == 0))
                {
                    btnConfirmar.Enabled = false;
                    lblMensagem.Text =
                        transferenciaCentral
                            ? "Não há estoque geral ativo de evento disponível para esta instituição."
                            : "Não há locais ou produtos disponíveis para uma transferência autorizada.";
                }
            }
            catch (Exception)
            {
                btnConfirmar.Enabled = false;
                lblMensagem.Text = "Não foi possível carregar os dados da transferência.";
            }
        }

        private void cmbOrigem_SelectedIndexChanged(object sender, EventArgs e)
        {
            AtualizarDestinos();
            MostrarSaldo();
        }

        private void cmbProduto_SelectedIndexChanged(object sender, EventArgs e)
        {
            MostrarSaldo();
        }

        private void AtualizarDestinos()
        {
            cmbDestino.Items.Clear();
            LocalEstoque origem = cmbOrigem.SelectedItem as LocalEstoque;

            if (origem == null || !origem.IdEvento.HasValue)
            {
                if (transferenciaCentral && origem != null && origem.EhCentral)
                {
                    cmbDestino.Items.Clear();
                    foreach (LocalEstoque local in locaisEvento)
                    {
                        if (local.Ativo && local.EhGeralEvento &&
                            local.IdInstituicao == origem.IdInstituicao)
                        {
                            cmbDestino.Items.Add(local);
                        }
                    }

                    if (cmbDestino.Items.Count > 0)
                    {
                        cmbDestino.SelectedIndex = 0;
                    }
                }
                return;
            }

            foreach (LocalEstoque local in locaisEvento)
            {
                if (local.IdLocalEstoque != origem.IdLocalEstoque &&
                    local.IdEvento == origem.IdEvento)
                {
                    cmbDestino.Items.Add(local);
                }
            }

            if (cmbDestino.Items.Count > 0)
            {
                cmbDestino.SelectedIndex = 0;
            }
        }

        private void MostrarSaldo()
        {

            LocalEstoque origem = cmbOrigem.SelectedItem as LocalEstoque;
            Produto produto = cmbProduto.SelectedItem as Produto;

            if (origem == null || produto == null || usuarioAutenticado == null)
            {
                lblSaldo.Text = "Disponível na origem: —";
                return;
            }

            try
            {
                List<Estoque> saldos = estoqueService.ListarPorLocal(
                    usuarioAutenticado,
                    origem.IdLocalEstoque);

                decimal quantidadeDisponivel = 0;

                foreach (Estoque estoque in saldos)
                {
                    if (estoque.IdProduto == produto.IdProduto)
                    {
                        quantidadeDisponivel = estoque.QuantidadeAtual;
                        break;
                    }
                }

                lblSaldo.Text = "Disponível na origem: " +
                    quantidadeDisponivel.ToString("N2") + " un";
            }
            catch (Exception)
            {
                lblSaldo.Text = "Saldo indisponível.";
            }
        }

        private void btnConfirmar_Click(object sender, EventArgs e)
        {
            ConfirmarTransferencia();
        }

        private void ConfirmarTransferencia()
        {

            if (salvando)
            {
                return;
            }

            LocalEstoque origem = cmbOrigem.SelectedItem as LocalEstoque;
            LocalEstoque destino = cmbDestino.SelectedItem as LocalEstoque;
            Produto produto = cmbProduto.SelectedItem as Produto;

            if (origem == null || destino == null || produto == null)
            {
                lblMensagem.Text = "Selecione a origem, o destino e o produto.";
                return;
            }

            if (numQuantidade.Value <= 0)
            {
                lblMensagem.Text = "A quantidade deve ser maior que zero.";
                return;
            }

            TransferenciaEstoque transferencia = new TransferenciaEstoque();
            transferencia.IdLocalOrigem = origem.IdLocalEstoque;
            transferencia.IdLocalDestino = destino.IdLocalEstoque;
            transferencia.DataHora = DateTime.Now;
            transferencia.Observacao = txtObservacao.Text.Trim();
            transferencia.ChaveOperacao = chaveOperacao;

            List<TransferenciaItem> itens = new List<TransferenciaItem>();
            TransferenciaItem item = new TransferenciaItem();
            item.IdProduto = produto.IdProduto;
            item.Quantidade = numQuantidade.Value;
            itens.Add(item);

            salvando = true;
            btnConfirmar.Enabled = false;

            try
            {
                transferenciaService.Registrar(usuarioAutenticado, transferencia, itens);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (ArgumentException ex)
            {
                CASE.Utils.FeedbackUsuario.ExibirErro(this, ex, "Confira os dados da transferência e tente novamente.", lblMensagem);
                ReabilitarConfirmacao();
            }
            catch (InvalidOperationException ex)
            {
                CASE.Utils.FeedbackUsuario.ExibirErro(this, ex, "Confira os dados da transferência e tente novamente.", lblMensagem);
                ReabilitarConfirmacao();
            }
            catch (UnauthorizedAccessException)
            {
                lblMensagem.Text = "Sua conta não pode realizar esta transferência.";
                ReabilitarConfirmacao();
            }
            catch (Exception erro)
            {
                System.Diagnostics.Trace.WriteLine(
                    erro.GetType().Name + " ao registrar transferência: " + erro.Message);
                CASE.Utils.FeedbackUsuario.ExibirErro(this, erro, "Não foi possível concluir a transferência. Confira os dados e tente novamente.", lblMensagem);
                ReabilitarConfirmacao();
            }
        }

        private void ReabilitarConfirmacao()
        {
            salvando = false;
            btnConfirmar.Enabled = true;
        }
    }
}
