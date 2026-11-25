namespace CASE
{
    partial class TodosEventosControl
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
            this.lblOrientacao = new System.Windows.Forms.Label();
            this.lista = new System.Windows.Forms.FlowLayoutPanel();
            this.lblAreaEventos = new System.Windows.Forms.Label();
            this.SuspendLayout();
            this.Name = "TodosEventosControl";
            this.BackColor = System.Drawing.Color.FromArgb(246, 248, 252);
            this.Size = new System.Drawing.Size(1100, 650);
            this.layout.Name = "layout";
            this.layout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layout.RowCount = 3;
            this.layout.ColumnCount = 1;
            this.layout.Padding = new System.Windows.Forms.Padding(24);
            this.layout.BackColor = System.Drawing.Color.FromArgb(246, 248, 252);
            this.layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Text = "Eventos disponíveis";
            this.lblTitulo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitulo.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            this.lblTitulo.ForeColor = System.Drawing.Color.FromArgb(16, 43, 73);
            this.layout.Controls.Add(this.lblTitulo, 0, 0);
            this.lblOrientacao.Name = "lblOrientacao";
            this.lblOrientacao.Text = "Escolha um evento para consultar os dados e acessar as ações disponíveis para sua conta.";
            this.lblOrientacao.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOrientacao.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblOrientacao.ForeColor = System.Drawing.Color.FromArgb(115, 132, 151);
            this.lblOrientacao.AutoEllipsis = true;
            this.layout.Controls.Add(this.lblOrientacao, 0, 1);
            this.lista.Name = "lista";
            this.lista.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lista.AutoScroll = true;
            this.lista.WrapContents = true;
            this.lista.Padding = new System.Windows.Forms.Padding(0, 0, 12, 12);
            this.lista.BackColor = System.Drawing.Color.FromArgb(246, 248, 252);
            this.lblAreaEventos.Name = "lblAreaEventos";
            this.lblAreaEventos.Text = "Os cartões dos eventos disponíveis para a conta aparecem nesta área.";
            this.lblAreaEventos.AutoSize = true;
            this.lblAreaEventos.Padding = new System.Windows.Forms.Padding(16);
            this.lblAreaEventos.ForeColor = System.Drawing.Color.FromArgb(115, 132, 151);
            this.lista.Controls.Add(this.lblAreaEventos);
            this.layout.Controls.Add(this.lista, 0, 2);
            this.Controls.Add(this.layout);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.TableLayoutPanel layout;
        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.Label lblOrientacao;
        private System.Windows.Forms.FlowLayoutPanel lista;
        private System.Windows.Forms.Label lblAreaEventos;
    }
}
