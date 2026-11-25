namespace CASE.Quermesse
{
    partial class DetalhesReposicaoForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.layout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitulo = new System.Windows.Forms.Label();
            this.lblSituacao = new System.Windows.Forms.Label();
            this.lblResumo = new System.Windows.Forms.Label();
            this.grade = new System.Windows.Forms.DataGridView();
            this.colproduto = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colquantidade = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colunidade = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.txtObservacoes = new System.Windows.Forms.TextBox();
            this.acoes = new System.Windows.Forms.FlowLayoutPanel();
            this.btnAprovar = new System.Windows.Forms.Button();
            this.btnRejeitar = new System.Windows.Forms.Button();
            this.btnFechar = new System.Windows.Forms.Button();
            this.SuspendLayout();
            this.Text = "Reposição do setor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Size = new System.Drawing.Size(840, 650);
            this.MinimumSize = new System.Drawing.Size(740, 580);
            this.ShowInTaskbar = false;
            this.MinimizeBox = false;
            this.layout.Name = "layout";
            this.layout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layout.ColumnCount = 1;
            this.layout.RowCount = 6;
            this.layout.Padding = new System.Windows.Forms.Padding(24);
            this.layout.BackColor = System.Drawing.Color.FromArgb(246, 248, 252);
            this.layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 94F));
            this.layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Text = "Reposição do setor";
            this.lblTitulo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitulo.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            this.lblTitulo.ForeColor = System.Drawing.Color.FromArgb(16, 43, 73);
            this.layout.Controls.Add(this.lblTitulo, 0, 0);
            this.lblSituacao.Name = "lblSituacao";
            this.lblSituacao.Text = "Situação do pedido";
            this.lblSituacao.Size = new System.Drawing.Size(180, 30);
            this.lblSituacao.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblSituacao.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblSituacao.BackColor = System.Drawing.Color.FromArgb(234, 242, 254);
            this.lblSituacao.ForeColor = System.Drawing.Color.FromArgb(23, 105, 210);
            this.layout.Controls.Add(this.lblSituacao, 0, 1);
            this.lblResumo.Name = "lblResumo";
            this.lblResumo.Text = "Evento, solicitante e locais do pedido";
            this.lblResumo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblResumo.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblResumo.ForeColor = System.Drawing.Color.FromArgb(115, 132, 151);
            this.lblResumo.Padding = System.Windows.Forms.Padding.Empty;
            this.layout.Controls.Add(this.lblResumo, 0, 2);
            this.grade.Name = "grade";
            this.grade.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grade.ReadOnly = true;
            this.grade.AllowUserToAddRows = false;
            this.grade.AllowUserToDeleteRows = false;
            this.grade.RowHeadersVisible = false;
            this.grade.BackgroundColor = System.Drawing.Color.White;
            this.grade.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grade.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grade.MultiSelect = false;
            this.colproduto.Name = "produto";
            this.colproduto.HeaderText = "Produto solicitado";
            this.colproduto.FillWeight = 220F;
            this.grade.Columns.Add(this.colproduto);
            this.colquantidade.Name = "quantidade";
            this.colquantidade.HeaderText = "Quantidade";
            this.colquantidade.FillWeight = 100F;
            this.grade.Columns.Add(this.colquantidade);
            this.colunidade.Name = "unidade";
            this.colunidade.HeaderText = "Unidade";
            this.colunidade.FillWeight = 100F;
            this.grade.Columns.Add(this.colunidade);
            this.layout.Controls.Add(this.grade, 0, 3);
            this.txtObservacoes.Name = "txtObservacoes";
            this.txtObservacoes.Multiline = true;
            this.txtObservacoes.ReadOnly = true;
            this.txtObservacoes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtObservacoes.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtObservacoes.Text = "Orientação e observações do pedido.";
            this.txtObservacoes.Name = "txtObservacoesReposicao";
            this.layout.Controls.Add(this.txtObservacoes, 0, 4);
            this.acoes.Name = "acoes";
            this.acoes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.acoes.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.acoes.WrapContents = false;
            this.acoes.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.btnAprovar.Name = "btnAprovar";
            this.btnAprovar.Text = "Aprovar pedido";
            this.btnAprovar.Size = new System.Drawing.Size(195, 38);
            this.btnAprovar.DialogResult = System.Windows.Forms.DialogResult.Yes;
            this.acoes.Controls.Add(this.btnAprovar);
            this.btnAprovar.BackColor = System.Drawing.Color.FromArgb(23, 105, 210);
            this.btnAprovar.ForeColor = System.Drawing.Color.White;
            this.btnRejeitar.Name = "btnRejeitar";
            this.btnRejeitar.Text = "Rejeitar pedido";
            this.btnRejeitar.Size = new System.Drawing.Size(140, 38);
            this.btnRejeitar.DialogResult = System.Windows.Forms.DialogResult.No;
            this.acoes.Controls.Add(this.btnRejeitar);
            this.btnFechar.Name = "btnFechar";
            this.btnFechar.Text = "Fechar";
            this.btnFechar.Size = new System.Drawing.Size(100, 38);
            this.btnFechar.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.acoes.Controls.Add(this.btnFechar);
            this.CancelButton = this.btnFechar;
            this.layout.Controls.Add(this.acoes, 0, 5);
            this.Controls.Add(this.layout);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.TableLayoutPanel layout;
        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.Label lblSituacao;
        private System.Windows.Forms.Label lblResumo;
        private System.Windows.Forms.DataGridView grade;
        private System.Windows.Forms.DataGridViewTextBoxColumn colproduto;
        private System.Windows.Forms.DataGridViewTextBoxColumn colquantidade;
        private System.Windows.Forms.DataGridViewTextBoxColumn colunidade;
        private System.Windows.Forms.TextBox txtObservacoes;
        private System.Windows.Forms.FlowLayoutPanel acoes;
        private System.Windows.Forms.Button btnAprovar;
        private System.Windows.Forms.Button btnRejeitar;
        private System.Windows.Forms.Button btnFechar;
    }
}
