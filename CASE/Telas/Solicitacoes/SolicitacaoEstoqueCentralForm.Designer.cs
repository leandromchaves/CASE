namespace CASE.Quermesse
{
    partial class SolicitacaoEstoqueCentralForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTipo = new System.Windows.Forms.Label();
            this.cmbTipo = new System.Windows.Forms.ComboBox();
            this.dgvItens = new System.Windows.Forms.DataGridView();
            this.colproduto = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.colquantidade = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnAdicionarItem = new System.Windows.Forms.Button();
            this.btnRemoverItem = new System.Windows.Forms.Button();
            this.btnEnviar = new System.Windows.Forms.Button();
            this.btnCancelar = new System.Windows.Forms.Button();
            this.lblAviso = new System.Windows.Forms.Label();
            this.SuspendLayout();
            this.Name = "SolicitacaoEstoqueCentralForm";
            this.Text = "Solicitação ao estoque central";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ClientSize = new System.Drawing.Size(660, 470);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BackColor = System.Drawing.Color.White;
            this.lblTipo.Name = "lblTipo";
            this.lblTipo.Text = "Tipo do pedido";
            this.lblTipo.Location = new System.Drawing.Point(20, 18);
            this.lblTipo.Size = new System.Drawing.Size(150, 22);
            this.cmbTipo.Name = "cmbTipo";
            this.cmbTipo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTipo.Name = "cmbTipoPedidoCentral";
            this.cmbTipo.Location = new System.Drawing.Point(20, 42);
            this.cmbTipo.Size = new System.Drawing.Size(610, 30);
            this.cmbTipo.Items.AddRange(new object[] { "Liberação do estoque central", "Devolução ao estoque central" });
            this.cmbTipo.SelectedIndex = 0;
            this.dgvItens.Name = "dgvItens";
            this.dgvItens.AllowUserToAddRows = false;
            this.dgvItens.AllowUserToDeleteRows = false;
            this.dgvItens.MultiSelect = false;
            this.dgvItens.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvItens.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvItens.RowHeadersVisible = false;
            this.dgvItens.BackgroundColor = System.Drawing.Color.White;
            this.dgvItens.EnableHeadersVisualStyles = false;
            this.dgvItens.Name = "dgvItensPedidoCentral";
            this.dgvItens.Location = new System.Drawing.Point(20, 88);
            this.dgvItens.Size = new System.Drawing.Size(610, 280);
            this.dgvItens.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.dgvItens_DataError);
            this.colproduto.Name = "produto";
            this.colproduto.HeaderText = "Produto";
            this.colproduto.FillWeight = 75F;
            this.dgvItens.Columns.Add(this.colproduto);
            this.colquantidade.Name = "quantidade";
            this.colquantidade.HeaderText = "Quantidade";
            this.colquantidade.FillWeight = 25F;
            this.dgvItens.Columns.Add(this.colquantidade);
            this.btnAdicionarItem.Name = "btnAdicionarItem";
            this.btnAdicionarItem.Text = "Adicionar produto";
            this.btnAdicionarItem.Location = new System.Drawing.Point(20, 378);
            this.btnAdicionarItem.Size = new System.Drawing.Size(140, 32);
            this.btnAdicionarItem.Name = "btnAdicionarItemCentral";
            this.btnAdicionarItem.Click += new System.EventHandler(this.btnAdicionarItem_Click);
            this.btnRemoverItem.Name = "btnRemoverItem";
            this.btnRemoverItem.Text = "Remover linha";
            this.btnRemoverItem.Location = new System.Drawing.Point(170, 378);
            this.btnRemoverItem.Size = new System.Drawing.Size(120, 32);
            this.btnRemoverItem.Name = "btnRemoverItemCentral";
            this.btnRemoverItem.Click += new System.EventHandler(this.btnRemoverItem_Click);
            this.btnEnviar.Name = "btnEnviar";
            this.btnEnviar.Text = "Enviar pedido";
            this.btnEnviar.Location = new System.Drawing.Point(415, 420);
            this.btnEnviar.Size = new System.Drawing.Size(105, 32);
            this.btnEnviar.Name = "btnEnviarPedidoCentral";
            this.btnEnviar.Click += new System.EventHandler(this.btnEnviar_Click);
            this.btnCancelar.Name = "btnCancelar";
            this.btnCancelar.Text = "Cancelar";
            this.btnCancelar.Location = new System.Drawing.Point(530, 420);
            this.btnCancelar.Size = new System.Drawing.Size(100, 32);
            this.btnCancelar.Name = "btnCancelarPedidoCentral";
            this.btnCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.lblAviso.Name = "lblAviso";
            this.lblAviso.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblAviso.Name = "lblAvisoPedidoCentral";
            this.lblAviso.Location = new System.Drawing.Point(20, 416);
            this.lblAviso.Size = new System.Drawing.Size(390, 36);
            this.Controls.Add(this.lblTipo);
            this.Controls.Add(this.cmbTipo);
            this.Controls.Add(this.dgvItens);
            this.Controls.Add(this.btnAdicionarItem);
            this.Controls.Add(this.btnRemoverItem);
            this.Controls.Add(this.lblAviso);
            this.Controls.Add(this.btnEnviar);
            this.Controls.Add(this.btnCancelar);
            this.AcceptButton = this.btnEnviar;
            this.CancelButton = this.btnCancelar;
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Label lblTipo;
        private System.Windows.Forms.ComboBox cmbTipo;
        private System.Windows.Forms.DataGridView dgvItens;
        private System.Windows.Forms.DataGridViewComboBoxColumn colproduto;
        private System.Windows.Forms.DataGridViewTextBoxColumn colquantidade;
        private System.Windows.Forms.Button btnAdicionarItem;
        private System.Windows.Forms.Button btnRemoverItem;
        private System.Windows.Forms.Button btnEnviar;
        private System.Windows.Forms.Button btnCancelar;
        private System.Windows.Forms.Label lblAviso;
    }
}
