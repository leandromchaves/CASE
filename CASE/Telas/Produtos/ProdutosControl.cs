using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE
{
    public partial class ProdutosControl : UserControl
    {
        private readonly ProdutoService produtoService = new ProdutoService();
        private readonly CategoriaService categoriaService = new CategoriaService();
        private readonly Usuario usuarioAutenticado;
        private List<Produto> produtos = new List<Produto>();
        private List<Categoria> categorias = new List<Categoria>();
        private readonly List<System.Drawing.Image> imagensDaGrade = new List<System.Drawing.Image>();

        public ProdutosControl() : this(null)
        {
        }

        public ProdutosControl(Usuario usuario)
        {
            InitializeComponent();
            usuarioAutenticado = usuario;

            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime)
            {
                return;
            }

            FrontendTheme.Aplicar(this);
            VisualUtils.Arredondar(panelAcoesProdutos, 12);
            VisualUtils.Arredondar(panelListaProdutos, 12);
            VisualUtils.Arredondar(btnNovoProduto, 8);
            VisualUtils.Arredondar(btnPaginaAnterior, 8);
            VisualUtils.Arredondar(btnProximaPagina, 8);
            VisualUtils.AplicarBorda(panelAcoesProdutos);
            VisualUtils.AplicarBorda(panelListaProdutos);

            panelAcoesProdutos.SizeChanged += AjustarArredondamento;
            panelListaProdutos.SizeChanged += AjustarArredondamento;

            colCodigo.Visible = false;

            DataGridViewImageColumn colunaImagem = new DataGridViewImageColumn();
            colunaImagem.Name = "colImagemProduto";
            colunaImagem.HeaderText = "Imagem";
            colunaImagem.ImageLayout = DataGridViewImageCellLayout.Zoom;
            colunaImagem.FillWeight = 65;
            colunaImagem.DefaultCellStyle.NullValue = null;
            dgvProdutos.Columns.Insert(0, colunaImagem);

            colAcoes.Text = "Editar";

            DataGridViewButtonColumn colunaExcluir = new DataGridViewButtonColumn();
            colunaExcluir.Name = "colExcluir";
            colunaExcluir.HeaderText = "";
            colunaExcluir.Text = "Excluir";
            colunaExcluir.UseColumnTextForButtonValue = true;
            colunaExcluir.FlatStyle = FlatStyle.Flat;
            colunaExcluir.FillWeight = 65;
            colunaExcluir.DefaultCellStyle.ForeColor = System.Drawing.Color.Firebrick;
            // A exclusão de produtos ainda não está disponível.
            colunaExcluir.Visible = false;
            dgvProdutos.Columns.Add(colunaExcluir);

            btnNovoProduto.Click += BtnNovoProduto_Click;
            VisualUtils.EstilizarPesquisa(txtPesquisarProduto, "Pesquisar produtos");
            dgvProdutos.CellContentClick += DgvProdutos_CellContentClick;
            txtPesquisarProduto.TextChanged += Filtros_Changed;
            cmbFiltroCategoria.SelectedIndexChanged += Filtros_Changed;
            ContextMenuStrip menuProduto = new ContextMenuStrip();
            ToolStripMenuItem fornecedores = new ToolStripMenuItem("Fornecedores do produto selecionado");
            fornecedores.Click += AbrirFornecedoresDoProduto;
            menuProduto.Items.Add(fornecedores);
            dgvProdutos.ContextMenuStrip = menuProduto;
            dgvProdutos.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
                {
                    dgvProdutos.CurrentCell = dgvProdutos.Rows[e.RowIndex].Cells[0];
                    dgvProdutos.Rows[e.RowIndex].Selected = true;
                }
            };
            Disposed += (s, e) => menuProduto.Dispose();

            btnPaginaAnterior.Enabled = false;
            btnProximaPagina.Enabled = false;
            dgvProdutos.ClearSelection();

            VisualUtils.ExibirEstadoVazio(
                dgvProdutos,
                "Nenhum produto cadastrado.");

            if (usuarioAutenticado != null)
            {
                CarregarProdutos();
                try
                {
                    string perfil;
                    new AutorizacaoService().ValidarUsuarioEObterPerfil(usuarioAutenticado, out perfil);
                    btnNovoProduto.Enabled = perfil == "Administrador";
                    if (perfil != "Administrador")
                    {
                        colAcoes.Text = "Consultar";
                    }
                }
                catch (Exception)
                {
                    btnNovoProduto.Enabled = false;
                }
            }
            else
            {
                btnNovoProduto.Enabled = false;
                lblPaginacao.Text = "Entre no sistema para consultar os produtos.";
            }
            Disposed += (s, e) => LiberarImagensDaGrade();
        }

        private void AbrirFornecedoresDoProduto(object sender, EventArgs e)
        {
            if (dgvProdutos.SelectedRows.Count == 0 || usuarioAutenticado == null)
            {
                return;
            }
            Produto produto = dgvProdutos.SelectedRows[0].Tag as Produto;
            if (produto == null)
            {
                return;
            }
            try
            {
                List<Fornecedor> fornecedores = new FornecedorService().Listar(usuarioAutenticado);
                List<ProdutoFornecedor> vinculos = produtoService.ListarFornecedores(usuarioAutenticado, produto.IdProduto);
                string perfil;
                new AutorizacaoService().ValidarUsuarioEObterPerfil(usuarioAutenticado, out perfil);
                using (Form janela = new Form())
                using (ComboBox fornecedor = new ComboBox())
                using (TextBox codigo = new TextBox())
                using (Button salvar = new Button())
                using (DataGridView grade = new DataGridView())
                {
                    janela.Text = "Fornecedores · " + produto.Nome;
                    janela.Name = "VinculosProdutoForm";
                    janela.ClientSize = new System.Drawing.Size(600, 430);
                    janela.MinimumSize = janela.Size;
                    janela.StartPosition = FormStartPosition.CenterParent;
                    IconoAplicacao.Aplicar(janela);
                    fornecedor.DisplayMember = "Nome";
                    fornecedor.Name = "cmbFornecedorVinculo";
                    fornecedor.DropDownStyle = ComboBoxStyle.DropDownList;
                    fornecedor.SetBounds(20, 48, 335, 28);
                    codigo.MaxLength = 50;
                    codigo.Name = "txtCodigoFornecedorVinculo";
                    codigo.SetBounds(375, 48, 205, 28);
                    codigo.ReadOnly = perfil != "Administrador";
                    salvar.Text = "Salvar vínculo";
                    salvar.Name = "btnSalvarVinculoFornecedor";
                    salvar.SetBounds(20, 93, 160, 36);
                    salvar.Enabled = perfil == "Administrador" && fornecedores.Count > 0;
                    Label mensagem = FrontendTheme.Legenda("Vínculos do catálogo. Não alteram preços de venda nem saldos.",
                        new System.Drawing.Rectangle(20, 145, 560, 42));
                    grade.SetBounds(20, 195, 560, 210);
                    grade.Name = "dgvVinculosFornecedor";
                    grade.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                    grade.AllowUserToAddRows = false;
                    grade.AllowUserToDeleteRows = false;
                    grade.ReadOnly = true;
                    grade.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    grade.Columns.Add("fornecedor", "Fornecedor");
                    grade.Columns.Add("codigo", "Código no fornecedor");
                    janela.Controls.Add(FrontendTheme.Legenda("Fornecedor", new System.Drawing.Rectangle(20, 20, 335, 22)));
                    janela.Controls.Add(FrontendTheme.Legenda("Código opcional", new System.Drawing.Rectangle(375, 20, 205, 22)));
                    janela.Controls.Add(fornecedor);
                    janela.Controls.Add(codigo);
                    janela.Controls.Add(salvar);
                    janela.Controls.Add(mensagem);
                    janela.Controls.Add(grade);
                    Action atualizar = () =>
                    {
                        vinculos = produtoService.ListarFornecedores(usuarioAutenticado, produto.IdProduto);
                        grade.Rows.Clear();
                        foreach (ProdutoFornecedor vinculo in vinculos)
                        {
                            foreach (Fornecedor item in fornecedores)
                            {
                                if (item.IdFornecedor == vinculo.IdFornecedor)
                                {
                                    grade.Rows.Add(item.Nome, vinculo.CodigoNoFornecedor);
                                    break;
                                }
                            }
                        }
                        VisualUtils.ExibirEstadoVazio(grade, "Nenhum fornecedor vinculado ao produto.");
                    };
                    fornecedor.SelectedIndexChanged += (s, argumentos) =>
                    {
                        codigo.Clear();
                        Fornecedor selecionado = fornecedor.SelectedItem as Fornecedor;
                        foreach (ProdutoFornecedor vinculo in vinculos)
                        {
                            if (selecionado != null && vinculo.IdFornecedor == selecionado.IdFornecedor)
                            {
                                codigo.Text = vinculo.CodigoNoFornecedor;
                                break;
                            }
                        }
                    };
                    salvar.Click += (s, argumentos) =>
                    {
                        Fornecedor selecionado = fornecedor.SelectedItem as Fornecedor;
                        if (selecionado == null)
                        {
                            mensagem.Text = "Selecione um fornecedor cadastrado.";
                            return;
                        }
                        try
                        {
                            produtoService.VincularFornecedor(usuarioAutenticado, produto.IdProduto,
                                selecionado.IdFornecedor, codigo.Text.Trim());
                            atualizar();
                            mensagem.Text = "Vínculo salvo no MySQL. Preços e saldos preservados.";
                        }
                        catch (Exception erro)
                        {
                            mensagem.Text = "Não foi possível salvar o vínculo.";
                            if (erro is ArgumentException || erro is UnauthorizedAccessException)
                            {
                                mensagem.Text = erro.Message;
                            }
                            System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em VincularFornecedor");
                        }
                    };
                    foreach (Fornecedor item in fornecedores)
                    {
                        fornecedor.Items.Add(item);
                    }
                    if (fornecedor.Items.Count > 0)
                    {
                        fornecedor.SelectedIndex = 0;
                    }
                    else
                    {
                        mensagem.Text = "Cadastre um fornecedor antes de vincular ao produto.";
                    }
                    FrontendTheme.Aplicar(janela);
                    atualizar();
                    janela.ShowDialog(this);
                }
            }
            catch (Exception erro)
            {
                MessageBox.Show(this, "Não foi possível consultar os fornecedores autorizados.", "Produtos",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em FornecedoresDoProduto");
            }
        }

        private void CarregarProdutos()
        {

            try
            {
                produtos = produtoService.Listar(usuarioAutenticado);
                categorias = categoriaService.Listar(usuarioAutenticado);
                CarregarFiltroDeCategorias();
                ExibirProdutos();
            }
            catch (Exception)
            {
                produtos = new List<Produto>();
                categorias = new List<Categoria>();
                dgvProdutos.Rows.Clear();
                lblPaginacao.Text = "Não foi possível carregar os produtos.";
            }
        }

        private void CarregarFiltroDeCategorias()
        {
            cmbFiltroCategoria.Items.Clear();
            cmbFiltroCategoria.Items.Add("Todas as categorias");

            foreach (Categoria categoria in categorias)
            {
                cmbFiltroCategoria.Items.Add(categoria.Nome);
            }

            cmbFiltroCategoria.SelectedIndex = 0;
        }

        private void ExibirProdutos()
        {
            dgvProdutos.Rows.Clear();
            LiberarImagensDaGrade();

            foreach (Produto produto in produtos)
            {
                string nomeCategoria = ObterNomeCategoria(produto.IdCategoria);

                object[] valores = new object[]
                {
                    CriarMiniatura(produto),
                    produto.IdProduto,
                    produto.Nome,
                    nomeCategoria,
                    "Não disponível nesta tela",
                    "Definido por setor"
                };

                int indiceLinha = dgvProdutos.Rows.Add(valores);
                dgvProdutos.Rows[indiceLinha].Tag = produto;
            }

            AplicarFiltros();
        }

        private System.Drawing.Image CriarMiniatura(Produto produto)
        {
            if (produto.Imagem == null || produto.Imagem.Length == 0)
            {
                return null;
            }
            try
            {
                using (var memoria = new System.IO.MemoryStream(produto.Imagem))
                using (var original = System.Drawing.Image.FromStream(memoria))
                {
                    double proporcao = Math.Min(64.0 / original.Width, 64.0 / original.Height);
                    var tamanho = new System.Drawing.Size(Math.Max(1, (int)(original.Width * proporcao)),
                        Math.Max(1, (int)(original.Height * proporcao)));
                    var miniatura = new System.Drawing.Bitmap(original, tamanho);
                    imagensDaGrade.Add(miniatura);
                    return miniatura;
                }
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private void LiberarImagensDaGrade()
        {
            foreach (System.Drawing.Image imagem in imagensDaGrade)
            {
                imagem.Dispose();
            }
            imagensDaGrade.Clear();
        }

        private string ObterNomeCategoria(int idCategoria)
        {
            foreach (Categoria categoria in categorias)
            {
                if (categoria.IdCategoria == idCategoria)
                {
                    return categoria.Nome;
                }
            }

            return "Categoria não encontrada";
        }

        private void AplicarFiltros()
        {
            dgvProdutos.CurrentCell = null;

            int quantidadeVisivel = 0;
            string textoBusca = txtPesquisarProduto.Text.Trim();

            foreach (DataGridViewRow linha in dgvProdutos.Rows)
            {
                Produto produto = linha.Tag as Produto;

                if (produto == null)
                {
                    linha.Visible = false;
                    continue;
                }

                string nomeCategoria = Convert.ToString(linha.Cells[colCategoria.Index].Value);
                bool nomeCorresponde = produto.Nome.IndexOf(
                    textoBusca,
                    StringComparison.CurrentCultureIgnoreCase) >= 0;
                bool categoriaCorresponde = cmbFiltroCategoria.SelectedIndex <= 0 ||
                    string.Equals(
                        nomeCategoria,
                        cmbFiltroCategoria.Text,
                        StringComparison.CurrentCultureIgnoreCase);

                linha.Visible = nomeCorresponde && categoriaCorresponde;

                if (linha.Visible)
                {
                    quantidadeVisivel++;
                }
            }

            lblPaginacao.Text = quantidadeVisivel + " de " + produtos.Count + " produtos";
            dgvProdutos.ClearSelection();
        }

        private void AbrirFormularioDeProduto(Produto produto)
        {

            using (ProdutoForm formulario = new ProdutoForm(usuarioAutenticado, produto))
            {
                if (formulario.ShowDialog(this) == DialogResult.OK)
                {
                    CarregarProdutos();
                }
            }
        }

        private void BtnNovoProduto_Click(object sender, EventArgs e)
        {

            using (ProdutoForm formulario = new ProdutoForm(usuarioAutenticado, null))
            {
                if (formulario.ShowDialog(this) == DialogResult.OK)
                {
                    CarregarProdutos();
                }
            }
        }

        private void DgvProdutos_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            Produto produto = dgvProdutos.Rows[e.RowIndex].Tag as Produto;

            if (produto == null)
            {
                return;
            }

            if (dgvProdutos.Columns[e.ColumnIndex] == colAcoes)
            {
                AbrirFormularioDeProduto(produto);
                return;
            }

            if (dgvProdutos.Columns[e.ColumnIndex].Name == "colExcluir")
            {
                MessageBox.Show(
                    this,
                    "A exclusão não está disponível para produtos nesta etapa.",
                    "Produtos",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void Filtros_Changed(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        public void Pesquisar(string texto)
        {
            if (texto == null)
            {
                texto = string.Empty;
            }

            txtPesquisarProduto.Text = texto;
        }

        private void AjustarArredondamento(object sender, EventArgs e)
        {
            Control controle = (Control)sender;
            VisualUtils.Arredondar(controle, 12);
            controle.Invalidate();
        }
    }
}
