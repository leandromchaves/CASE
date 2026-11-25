using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE
{
    public partial class ProdutosControl : UserControl
    {
        private ProdutoService produtoService = new ProdutoService();
        private CategoriaService categoriaService = new CategoriaService();
        private Usuario usuarioAutenticado;
        private List<Produto> produtos = new List<Produto>();
        private List<Categoria> categorias = new List<Categoria>();
        private List<Image> imagensDaGrade = new List<Image>();
        private CheckBox mostrarRemovidos = new CheckBox();
        public ProdutosControl() : this(null)
        {
        }

        public ProdutosControl(Usuario usuario)
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;
            usuarioAutenticado = usuario;
            lblSubtituloProdutos.Text = "Gerencie o catálogo. Preços de venda são definidos por setor em Equipes e setores.";
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                return;
            }

            FrontendTheme.Aplicar(this);
            mostrarRemovidos.Name = "chkIncluirProdutosRemovidos";
            mostrarRemovidos.Text = "Incluir produtos removidos";
            mostrarRemovidos.Font = dgvProdutos.DefaultCellStyle.Font;
            mostrarRemovidos.ForeColor = VisualUtils.Texto;
            mostrarRemovidos.SetBounds(panelListaProdutos.Width - 235, 18, 215, 26);
            mostrarRemovidos.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            mostrarRemovidos.CheckedChanged += Filtros_Changed;
            panelListaProdutos.Controls.Add(mostrarRemovidos);
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
            colQuantidade.Visible = false;
            DataGridViewImageColumn colunaImagem = new DataGridViewImageColumn();
            colunaImagem.Name = "colImagemProduto";
            colunaImagem.HeaderText = "Imagem";
            colunaImagem.ImageLayout = DataGridViewImageCellLayout.Zoom;
            colunaImagem.FillWeight = 8;
            colunaImagem.MinimumWidth = 76;
            colunaImagem.DefaultCellStyle.NullValue = null;
            colunaImagem.DefaultCellStyle.Padding = new Padding(6);
            colunaImagem.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvProdutos.Columns.Insert(0, colunaImagem);
            colAcoes.Text = "Editar";
            DataGridViewButtonColumn colunaExcluir = new DataGridViewButtonColumn();
            colunaExcluir.Name = "colExcluir";
            colunaExcluir.HeaderText = "";
            colunaExcluir.Text = "";
            colunaExcluir.UseColumnTextForButtonValue = false;
            colunaExcluir.FlatStyle = FlatStyle.Flat;
            colunaExcluir.FillWeight = 14;
            colunaExcluir.DefaultCellStyle.ForeColor = Color.FromArgb(185, 48, 48);
            colunaExcluir.Visible = false;
            dgvProdutos.Columns.Add(colunaExcluir);
            ConfigurarApresentacaoDaGrade();
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
            dgvProdutos.CellMouseDown += dgvProdutos_CellMouseDown;
            Disposed += Tratar_Disposed;
            btnPaginaAnterior.Enabled = false;
            btnProximaPagina.Enabled = false;
            dgvProdutos.ClearSelection();
            VisualUtils.ExibirEstadoVazio(dgvProdutos, "Nenhum produto cadastrado.");
            if (usuarioAutenticado != null)
            {
                CarregarProdutos();
                try
                {
                    string perfil;
                    new AutorizacaoService().ValidarUsuarioEObterPerfil(usuarioAutenticado, out perfil);
                    btnNovoProduto.Enabled = perfil == "Administrador";
                    colunaExcluir.Visible = perfil == "Administrador";
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

            Disposed += Tela_Disposed;
            void Tratar_Disposed(object s, EventArgs e)
            {
                menuProduto.Dispose();
            }
        }

        private void ConfigurarApresentacaoDaGrade()
        {
            // O Designer compartilhava este estilo com a coluna de ações. Separar as
            // instâncias impede que centralizar o botão "Editar" centralize também
            // todas as células das linhas alternadas.
            dgvProdutos.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle(dgvProdutos.AlternatingRowsDefaultCellStyle);
            colAcoes.DefaultCellStyle = new DataGridViewCellStyle(colAcoes.DefaultCellStyle);
            dgvProdutos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProdutos.RowTemplate.Height = 68;
            dgvProdutos.ColumnHeadersHeight = 44;
            dgvProdutos.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvProdutos.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);
            dgvProdutos.AlternatingRowsDefaultCellStyle.Font = dgvProdutos.DefaultCellStyle.Font;
            dgvProdutos.AlternatingRowsDefaultCellStyle.Padding = dgvProdutos.DefaultCellStyle.Padding;
            dgvProdutos.AlternatingRowsDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvProdutos.ColumnHeadersDefaultCellStyle.Font = dgvProdutos.DefaultCellStyle.Font;
            dgvProdutos.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvProdutos.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 10, 0);
            colProduto.FillWeight = 30;
            colCategoria.FillWeight = 22;
            colPreco.FillWeight = 22;
            colAcoes.FillWeight = 14;
            foreach (DataGridViewColumn coluna in dgvProdutos.Columns)
            {
                coluna.DefaultCellStyle.Font = dgvProdutos.DefaultCellStyle.Font;
                coluna.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }

            colAcoes.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colAcoes.DefaultCellStyle.Padding = new Padding(6, 8, 6, 8);
            dgvProdutos.RowTemplate.Height = 68;
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

            GerenciarFornecedores(this, usuarioAutenticado, produto);
        }

        internal static void GerenciarFornecedores(IWin32Window proprietario, Usuario usuarioAutenticado, Produto produto)
        {
            ProdutoService produtoService = new ProdutoService();
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
                using (Button remover = new Button())
                using (DataGridView grade = new DataGridView())
                {
                    janela.Text = "Fornecedores · " + produto.Nome;
                    janela.Name = "VinculosProdutoForm";
                    janela.ClientSize = new Size(600, 430);
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
                    salvar.BackColor = VisualUtils.Azul;
                    salvar.ForeColor = Color.White;
                    salvar.Name = "btnSalvarVinculoFornecedor";
                    salvar.SetBounds(20, 93, 160, 36);
                    salvar.Enabled = perfil == "Administrador" && fornecedores.Count > 0;
                    remover.Text = "Remover vínculo";
                    remover.Name = "btnRemoverVinculoFornecedor";
                    remover.SetBounds(195, 93, 160, 36);
                    remover.Enabled = false;
                    Label mensagem = FrontendTheme.Legenda("Vínculos do catálogo. Não alteram preços de venda nem saldos.", new Rectangle(20, 145, 560, 42));
                    grade.SetBounds(20, 195, 560, 210);
                    grade.Name = "dgvVinculosFornecedor";
                    grade.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                    grade.AllowUserToAddRows = false;
                    grade.AllowUserToDeleteRows = false;
                    grade.ReadOnly = true;
                    grade.RowHeadersVisible = false;
                    grade.BackgroundColor = Color.White;
                    grade.BorderStyle = BorderStyle.None;
                    grade.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    grade.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                    grade.MultiSelect = false;
                    grade.Columns.Add("fornecedor", "Fornecedor");
                    grade.Columns.Add("codigo", "Código no fornecedor");
                    janela.Controls.Add(FrontendTheme.Legenda("Fornecedor", new Rectangle(20, 20, 335, 22)));
                    janela.Controls.Add(FrontendTheme.Legenda("Código opcional", new Rectangle(375, 20, 205, 22)));
                    janela.Controls.Add(fornecedor);
                    janela.Controls.Add(codigo);
                    janela.Controls.Add(salvar);
                    janela.Controls.Add(remover);
                    janela.Controls.Add(mensagem);
                    janela.Controls.Add(grade);
                    grade.SelectionChanged += Tratar_grade_SelectionChanged;
                    grade.CellClick += Tratar_grade_CellClick;
                    Action sincronizarSelecaoGrade = Tratar_GerenciarFornecedores_Executar;
                    Action atualizar = Tratar_GerenciarFornecedores_Executar2;
                    fornecedor.SelectedIndexChanged += Tratar_fornecedor_SelectedIndexChanged;
                    salvar.Click += Tratar_salvar_Click;
                    remover.Click += Tratar_remover_Click;
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
                    janela.ShowDialog(proprietario);
                    void Tratar_grade_SelectionChanged(object s, EventArgs argumentos)
                    {
                        remover.Enabled = perfil == "Administrador" && grade.SelectedRows.Count == 1 && grade.SelectedRows[0].Tag != null;
                    }

                    void Tratar_grade_CellClick(object s, DataGridViewCellEventArgs argumentos)
                    {
                        if (argumentos.RowIndex < 0)
                            return;
                        int idFornecedor = Convert.ToInt32(grade.Rows[argumentos.RowIndex].Tag);
                        foreach (Fornecedor item in fornecedor.Items)
                            if (item.IdFornecedor == idFornecedor)
                            {
                                fornecedor.SelectedItem = item;
                                break;
                            }
                    }

                    void Tratar_GerenciarFornecedores_Executar()
                    {
                        grade.ClearSelection();
                        grade.CurrentCell = null;
                        Fornecedor selecionado = fornecedor.SelectedItem as Fornecedor;
                        if (selecionado == null)
                        {
                            return;
                        }

                        foreach (DataGridViewRow linha in grade.Rows)
                        {
                            if (linha.Tag != null && Convert.ToInt32(linha.Tag) == selecionado.IdFornecedor)
                            {
                                linha.Selected = true;
                                grade.CurrentCell = linha.Cells[0];
                                return;
                            }
                        }
                    }

                    void Tratar_GerenciarFornecedores_Executar2()
                    {
                        vinculos = produtoService.ListarFornecedores(usuarioAutenticado, produto.IdProduto);
                        grade.Rows.Clear();
                        foreach (ProdutoFornecedor vinculo in vinculos)
                        {
                            foreach (Fornecedor item in fornecedores)
                            {
                                if (item.IdFornecedor == vinculo.IdFornecedor)
                                {
                                    int indice = grade.Rows.Add(item.Nome, vinculo.CodigoNoFornecedor);
                                    grade.Rows[indice].Tag = item.IdFornecedor;
                                    break;
                                }
                            }
                        }

                        VisualUtils.ExibirEstadoVazio(grade, "Nenhum fornecedor vinculado ao produto.");
                        sincronizarSelecaoGrade();
                    }

                    void Tratar_fornecedor_SelectedIndexChanged(object s, EventArgs argumentos)
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

                        sincronizarSelecaoGrade();
                    }

                    void Tratar_salvar_Click(object s, EventArgs argumentos)
                    {
                        Fornecedor selecionado = fornecedor.SelectedItem as Fornecedor;
                        if (selecionado == null)
                        {
                            mensagem.Text = "Selecione um fornecedor cadastrado.";
                            return;
                        }

                        try
                        {
                            produtoService.VincularFornecedor(usuarioAutenticado, produto.IdProduto, selecionado.IdFornecedor, codigo.Text.Trim());
                            atualizar();
                            mensagem.Text = "Fornecedor vinculado ao produto.";
                            FeedbackUsuario.Exibir(janela, mensagem.Text, TipoFeedback.Sucesso);
                        }
                        catch (Exception erro)
                        {
                            mensagem.Text = "Não foi possível salvar o vínculo.";
                            if (erro is ArgumentException || erro is UnauthorizedAccessException)
                            {
                                mensagem.Text = FeedbackUsuario.MensagemErro(erro, "Não foi possível salvar o vínculo.");
                            }

                            FeedbackUsuario.Exibir(janela, mensagem.Text, TipoFeedback.Erro);
                            Trace.WriteLine(erro.GetType().Name + " em VincularFornecedor");
                        }
                    }

                    void Tratar_remover_Click(object s, EventArgs argumentos)
                    {
                        if (grade.SelectedRows.Count != 1 || grade.SelectedRows[0].Tag == null)
                            return;
                        DataGridViewRow linha = grade.SelectedRows[0];
                        int idFornecedor = Convert.ToInt32(linha.Tag);
                        string nomeFornecedor = Convert.ToString(linha.Cells[0].Value);
                        if (MessageBox.Show(janela, "Remover o vínculo de \"" + nomeFornecedor + "\" com o produto \"" + produto.Nome + "\"?\n\n" + "O fornecedor continuará cadastrado. Os outros vínculos e os registros de compras não serão alterados.", "Remover fornecedor do produto", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                            return;
                        try
                        {
                            bool removido = produtoService.DesvincularFornecedor(usuarioAutenticado, produto.IdProduto, idFornecedor);
                            atualizar();
                            codigo.Clear();
                            mensagem.Text = removido ? "Fornecedor removido deste produto." : "Este vínculo já foi removido.";
                            FeedbackUsuario.Exibir(janela, mensagem.Text, TipoFeedback.Sucesso);
                        }
                        catch (Exception erro)
                        {
                            FeedbackUsuario.ExibirErro(janela, erro, "Não foi possível remover o vínculo. Tente novamente.", mensagem);
                        }
                    }
                }
            }
            catch (Exception erro)
            {
                MessageBox.Show(proprietario, "Não foi possível consultar os fornecedores autorizados.", "Produtos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Trace.WriteLine(erro.GetType().Name + " em FornecedoresDoProduto");
            }
        }

        private void CarregarProdutos()
        {
            try
            {
                List<Produto> novosProdutos = produtoService.Listar(usuarioAutenticado);
                List<Categoria> novasCategorias = categoriaService.Listar(usuarioAutenticado);
                produtos = novosProdutos;
                categorias = novasCategorias;
                CarregarFiltroDeCategorias();
                ExibirProdutos();
            }
            catch (Exception)
            {
                lblPaginacao.Text = produtos.Count == 0 ? "Não foi possível carregar os produtos. Tente novamente." : produtos.Count + " produtos em exibição. A atualização falhou; tente novamente.";
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
                    null,
                    "Definido por setor"
                };
                int indiceLinha = dgvProdutos.Rows.Add(valores);
                dgvProdutos.Rows[indiceLinha].Tag = produto;
                dgvProdutos.Rows[indiceLinha].Cells["colExcluir"].Value = produto.Ativo ? "Remover" : "Reativar";
                if (!produto.Ativo)
                {
                    dgvProdutos.Rows[indiceLinha].DefaultCellStyle.ForeColor = Color.Gray;
                }
            }

            AplicarFiltros();
        }

        private Image CriarMiniatura(Produto produto)
        {
            if (produto.Imagem == null || produto.Imagem.Length == 0)
            {
                return null;
            }

            try
            {
                using (var memoria = new MemoryStream(produto.Imagem))
                using (var original = Image.FromStream(memoria))
                {
                    const int tamanhoMiniatura = 56;
                    const int margemImagem = 7;
                    var miniatura = new Bitmap(tamanhoMiniatura, tamanhoMiniatura, PixelFormat.Format32bppArgb);
                    using (var desenho = Graphics.FromImage(miniatura))
                    using (var fundo = new SolidBrush(Color.White))
                    using (var borda = new Pen(Color.FromArgb(226, 232, 240)))
                    using (var contorno = VisualUtils.CriarRetanguloArredondado(new Rectangle(0, 0, tamanhoMiniatura - 1, tamanhoMiniatura - 1), 9))
                    {
                        desenho.Clear(Color.Transparent);
                        desenho.SmoothingMode = SmoothingMode.AntiAlias;
                        desenho.FillPath(fundo, contorno);
                        desenho.SetClip(contorno);
                        double escala = Math.Min((double)(tamanhoMiniatura - margemImagem * 2) / original.Width, (double)(tamanhoMiniatura - margemImagem * 2) / original.Height);
                        int largura = Math.Max(1, (int)Math.Round(original.Width * escala));
                        int altura = Math.Max(1, (int)Math.Round(original.Height * escala));
                        int x = (tamanhoMiniatura - largura) / 2;
                        int y = (tamanhoMiniatura - altura) / 2;
                        desenho.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        desenho.DrawImage(original, new Rectangle(x, y, largura, altura));
                        desenho.ResetClip();
                        desenho.DrawPath(borda, contorno);
                    }

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
            foreach (Image imagem in imagensDaGrade)
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
                bool nomeCorresponde = produto.Nome.IndexOf(textoBusca, StringComparison.CurrentCultureIgnoreCase) >= 0;
                bool categoriaCorresponde = cmbFiltroCategoria.SelectedIndex <= 0 || string.Equals(nomeCategoria, cmbFiltroCategoria.Text, StringComparison.CurrentCultureIgnoreCase);
                linha.Visible = nomeCorresponde && categoriaCorresponde && (produto.Ativo || mostrarRemovidos.Checked);
                if (linha.Visible)
                {
                    quantidadeVisivel++;
                }
            }

            lblPaginacao.Text = quantidadeVisivel + " produto(s) exibido(s)";
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
                bool ativar = !produto.Ativo;
                string acao = ativar ? "reativar" : "remover";
                string explicacao = ativar ? "O produto voltará a aparecer para novas operações." : "O produto deixará de aparecer para novas operações. O histórico e os registros existentes serão preservados.";
                if (MessageBox.Show(this, "Deseja " + acao + " “" + produto.Nome + "”?\n\n" + explicacao, "Produtos", MessageBoxButtons.YesNo, ativar ? MessageBoxIcon.Question : MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }

                try
                {
                    produtoService.AlterarAtivo(usuarioAutenticado, produto.IdProduto, ativar);
                    CarregarProdutos();
                    lblPaginacao.Text = ativar ? "Produto reativado. Ele já está disponível para novas operações." : "Produto removido do catálogo ativo. O histórico foi preservado.";
                }
                catch (Exception erro)
                {
                    FeedbackUsuario.ExibirErro(this, erro, "Não foi possível atualizar o produto. Tente novamente.", lblPaginacao);
                }
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

        private void dgvProdutos_CellMouseDown(object s, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                dgvProdutos.CurrentCell = dgvProdutos.Rows[e.RowIndex].Cells[0];
                dgvProdutos.Rows[e.RowIndex].Selected = true;
            }
        }

        private void Tela_Disposed(object s, EventArgs e)
        {
            LiberarImagensDaGrade();
        }
    }
}
