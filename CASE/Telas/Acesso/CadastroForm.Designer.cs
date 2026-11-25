namespace CASE
{
    partial class CadastroForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.panelCadastro = new System.Windows.Forms.Panel();
            this.lblTituloCadastro = new System.Windows.Forms.Label();
            this.lblSubtituloCadastro = new System.Windows.Forms.Label();
            this.lblFotoOpcional = new System.Windows.Forms.Label();
            this.pictureFotoCadastro = new System.Windows.Forms.PictureBox();
            this.menuFoto = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.itemRemoverFoto = new System.Windows.Forms.ToolStripMenuItem();
            this.btnSelecionarFoto = new System.Windows.Forms.Button();
            this.lblNomeCompleto = new System.Windows.Forms.Label();
            this.panelNomeCompleto = new System.Windows.Forms.Panel();
            this.txtNomeCompleto = new System.Windows.Forms.TextBox();
            this.lblUsuario = new System.Windows.Forms.Label();
            this.panelUsuarioCadastro = new System.Windows.Forms.Panel();
            this.txtUsuario = new System.Windows.Forms.TextBox();
            this.lblSenha = new System.Windows.Forms.Label();
            this.panelSenhaCadastro = new System.Windows.Forms.Panel();
            this.txtSenha = new System.Windows.Forms.TextBox();
            this.lblConfirmarSenha = new System.Windows.Forms.Label();
            this.panelConfirmarSenha = new System.Windows.Forms.Panel();
            this.txtConfirmarSenha = new System.Windows.Forms.TextBox();
            this.chkMostrarSenha = new System.Windows.Forms.CheckBox();
            this.lblMensagemCadastro = new System.Windows.Forms.Label();
            this.btnCadastrar = new System.Windows.Forms.Button();
            this.linkVoltarLogin = new System.Windows.Forms.LinkLabel();
            this.panelCadastro.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureFotoCadastro)).BeginInit();
            this.menuFoto.SuspendLayout();
            this.panelNomeCompleto.SuspendLayout();
            this.panelUsuarioCadastro.SuspendLayout();
            this.panelSenhaCadastro.SuspendLayout();
            this.panelConfirmarSenha.SuspendLayout();
            this.SuspendLayout();
            //
            // panelCadastro
            //
            this.panelCadastro.BackColor = System.Drawing.Color.White;
            this.panelCadastro.Controls.Add(this.lblTituloCadastro);
            this.panelCadastro.Controls.Add(this.lblSubtituloCadastro);
            this.panelCadastro.Controls.Add(this.lblFotoOpcional);
            this.panelCadastro.Controls.Add(this.pictureFotoCadastro);
            this.panelCadastro.Controls.Add(this.btnSelecionarFoto);
            this.panelCadastro.Controls.Add(this.lblNomeCompleto);
            this.panelCadastro.Controls.Add(this.panelNomeCompleto);
            this.panelCadastro.Controls.Add(this.lblUsuario);
            this.panelCadastro.Controls.Add(this.panelUsuarioCadastro);
            this.panelCadastro.Controls.Add(this.lblSenha);
            this.panelCadastro.Controls.Add(this.panelSenhaCadastro);
            this.panelCadastro.Controls.Add(this.lblConfirmarSenha);
            this.panelCadastro.Controls.Add(this.panelConfirmarSenha);
            this.panelCadastro.Controls.Add(this.chkMostrarSenha);
            this.panelCadastro.Controls.Add(this.lblMensagemCadastro);
            this.panelCadastro.Controls.Add(this.btnCadastrar);
            this.panelCadastro.Controls.Add(this.linkVoltarLogin);
            this.panelCadastro.Location = new System.Drawing.Point(636, 82);
            this.panelCadastro.Name = "panelCadastro";
            this.panelCadastro.Size = new System.Drawing.Size(520, 655);
            this.panelCadastro.TabIndex = 0;
            //
            // lblTituloCadastro
            //
            this.lblTituloCadastro.BackColor = System.Drawing.Color.White;
            this.lblTituloCadastro.Font = new System.Drawing.Font("Segoe UI", 21F, System.Drawing.FontStyle.Bold);
            this.lblTituloCadastro.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(16)))), ((int)(((byte)(43)))), ((int)(((byte)(73)))));
            this.lblTituloCadastro.Location = new System.Drawing.Point(32, 24);
            this.lblTituloCadastro.Name = "lblTituloCadastro";
            this.lblTituloCadastro.Size = new System.Drawing.Size(456, 40);
            this.lblTituloCadastro.TabIndex = 0;
            this.lblTituloCadastro.Text = "Criar sua conta";
            //
            // lblSubtituloCadastro
            //
            this.lblSubtituloCadastro.BackColor = System.Drawing.Color.White;
            this.lblSubtituloCadastro.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblSubtituloCadastro.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(115)))), ((int)(((byte)(132)))), ((int)(((byte)(151)))));
            this.lblSubtituloCadastro.Location = new System.Drawing.Point(32, 67);
            this.lblSubtituloCadastro.Name = "lblSubtituloCadastro";
            this.lblSubtituloCadastro.Size = new System.Drawing.Size(456, 36);
            this.lblSubtituloCadastro.TabIndex = 1;
            this.lblSubtituloCadastro.Text = "Solicite acesso ao CASE. O cadastro será analisado pela instituição.";
            //
            // lblFotoOpcional
            //
            this.lblFotoOpcional.BackColor = System.Drawing.Color.White;
            this.lblFotoOpcional.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblFotoOpcional.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(115)))), ((int)(((byte)(132)))), ((int)(((byte)(151)))));
            this.lblFotoOpcional.Location = new System.Drawing.Point(236, 124);
            this.lblFotoOpcional.Name = "lblFotoOpcional";
            this.lblFotoOpcional.Size = new System.Drawing.Size(252, 26);
            this.lblFotoOpcional.TabIndex = 14;
            this.lblFotoOpcional.Text = "Foto opcional";
            this.lblFotoOpcional.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // pictureFotoCadastro
            //
            this.pictureFotoCadastro.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(244)))), ((int)(((byte)(251)))));
            this.pictureFotoCadastro.ContextMenuStrip = this.menuFoto;
            this.pictureFotoCadastro.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pictureFotoCadastro.Image = global::CASE.Properties.Resources.IconeAzulUsuario;
            this.pictureFotoCadastro.Location = new System.Drawing.Point(32, 111);
            this.pictureFotoCadastro.Name = "pictureFotoCadastro";
            this.pictureFotoCadastro.Size = new System.Drawing.Size(52, 52);
            this.pictureFotoCadastro.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureFotoCadastro.TabIndex = 13;
            this.pictureFotoCadastro.TabStop = false;
            this.pictureFotoCadastro.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PictureFotoCadastro_MouseUp);
            //
            // menuFoto
            //
            this.menuFoto.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.itemRemoverFoto});
            this.menuFoto.Name = "menuFoto";
            this.menuFoto.Size = new System.Drawing.Size(61, 4);
            //
            // itemRemoverFoto
            //
            this.itemRemoverFoto.Name = "itemRemoverFoto";
            this.itemRemoverFoto.Size = new System.Drawing.Size(32, 19);
            this.itemRemoverFoto.Text = "Remover foto selecionada";
            this.itemRemoverFoto.Click += new System.EventHandler(this.RemoverFoto_Click);
            //
            // btnSelecionarFoto
            //
            this.btnSelecionarFoto.BackColor = System.Drawing.Color.White;
            this.btnSelecionarFoto.FlatAppearance.BorderSize = 0;
            this.btnSelecionarFoto.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSelecionarFoto.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(51)))), ((int)(((byte)(75)))));
            this.btnSelecionarFoto.Location = new System.Drawing.Point(98, 120);
            this.btnSelecionarFoto.Name = "btnSelecionarFoto";
            this.btnSelecionarFoto.Size = new System.Drawing.Size(128, 34);
            this.btnSelecionarFoto.TabIndex = 5;
            this.btnSelecionarFoto.Text = "Escolher foto";
            this.btnSelecionarFoto.UseVisualStyleBackColor = false;
            this.btnSelecionarFoto.Click += new System.EventHandler(this.SelecionarFoto_Click);
            //
            // lblNomeCompleto
            //
            this.lblNomeCompleto.BackColor = System.Drawing.Color.Transparent;
            this.lblNomeCompleto.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblNomeCompleto.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(51)))), ((int)(((byte)(75)))));
            this.lblNomeCompleto.Location = new System.Drawing.Point(32, 178);
            this.lblNomeCompleto.Name = "lblNomeCompleto";
            this.lblNomeCompleto.Size = new System.Drawing.Size(456, 19);
            this.lblNomeCompleto.TabIndex = 2;
            this.lblNomeCompleto.Text = "Nome completo *";
            //
            // panelNomeCompleto
            //
            this.panelNomeCompleto.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.panelNomeCompleto.Controls.Add(this.txtNomeCompleto);
            this.panelNomeCompleto.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.panelNomeCompleto.Location = new System.Drawing.Point(32, 201);
            this.panelNomeCompleto.Name = "panelNomeCompleto";
            this.panelNomeCompleto.Padding = new System.Windows.Forms.Padding(12, 2, 12, 2);
            this.panelNomeCompleto.Size = new System.Drawing.Size(456, 42);
            this.panelNomeCompleto.TabIndex = 0;
            //
            // txtNomeCompleto
            //
            this.txtNomeCompleto.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.txtNomeCompleto.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtNomeCompleto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtNomeCompleto.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtNomeCompleto.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(16)))), ((int)(((byte)(43)))), ((int)(((byte)(73)))));
            this.txtNomeCompleto.Location = new System.Drawing.Point(12, 2);
            this.txtNomeCompleto.Margin = new System.Windows.Forms.Padding(0);
            this.txtNomeCompleto.Name = "txtNomeCompleto";
            this.txtNomeCompleto.Size = new System.Drawing.Size(432, 18);
            this.txtNomeCompleto.TabIndex = 0;
            //
            // lblUsuario
            //
            this.lblUsuario.BackColor = System.Drawing.Color.Transparent;
            this.lblUsuario.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblUsuario.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(51)))), ((int)(((byte)(75)))));
            this.lblUsuario.Location = new System.Drawing.Point(32, 253);
            this.lblUsuario.Name = "lblUsuario";
            this.lblUsuario.Size = new System.Drawing.Size(456, 19);
            this.lblUsuario.TabIndex = 4;
            this.lblUsuario.Text = "Nome de usuário *";
            //
            // panelUsuarioCadastro
            //
            this.panelUsuarioCadastro.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.panelUsuarioCadastro.Controls.Add(this.txtUsuario);
            this.panelUsuarioCadastro.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.panelUsuarioCadastro.Location = new System.Drawing.Point(32, 276);
            this.panelUsuarioCadastro.Name = "panelUsuarioCadastro";
            this.panelUsuarioCadastro.Padding = new System.Windows.Forms.Padding(12, 2, 12, 2);
            this.panelUsuarioCadastro.Size = new System.Drawing.Size(456, 42);
            this.panelUsuarioCadastro.TabIndex = 1;
            //
            // txtUsuario
            //
            this.txtUsuario.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.txtUsuario.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtUsuario.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtUsuario.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtUsuario.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(16)))), ((int)(((byte)(43)))), ((int)(((byte)(73)))));
            this.txtUsuario.Location = new System.Drawing.Point(12, 2);
            this.txtUsuario.Margin = new System.Windows.Forms.Padding(0);
            this.txtUsuario.Name = "txtUsuario";
            this.txtUsuario.Size = new System.Drawing.Size(432, 18);
            this.txtUsuario.TabIndex = 0;
            //
            // lblSenha
            //
            this.lblSenha.BackColor = System.Drawing.Color.Transparent;
            this.lblSenha.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSenha.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(51)))), ((int)(((byte)(75)))));
            this.lblSenha.Location = new System.Drawing.Point(32, 328);
            this.lblSenha.Name = "lblSenha";
            this.lblSenha.Size = new System.Drawing.Size(356, 19);
            this.lblSenha.TabIndex = 8;
            this.lblSenha.Text = "Senha *";
            //
            // panelSenhaCadastro
            //
            this.panelSenhaCadastro.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.panelSenhaCadastro.Controls.Add(this.txtSenha);
            this.panelSenhaCadastro.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.panelSenhaCadastro.Location = new System.Drawing.Point(32, 351);
            this.panelSenhaCadastro.Name = "panelSenhaCadastro";
            this.panelSenhaCadastro.Padding = new System.Windows.Forms.Padding(12, 2, 12, 2);
            this.panelSenhaCadastro.Size = new System.Drawing.Size(456, 42);
            this.panelSenhaCadastro.TabIndex = 2;
            //
            // txtSenha
            //
            this.txtSenha.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.txtSenha.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtSenha.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSenha.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtSenha.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(16)))), ((int)(((byte)(43)))), ((int)(((byte)(73)))));
            this.txtSenha.Location = new System.Drawing.Point(12, 2);
            this.txtSenha.Margin = new System.Windows.Forms.Padding(0);
            this.txtSenha.Name = "txtSenha";
            this.txtSenha.Size = new System.Drawing.Size(432, 18);
            this.txtSenha.TabIndex = 0;
            this.txtSenha.UseSystemPasswordChar = true;
            //
            // lblConfirmarSenha
            //
            this.lblConfirmarSenha.BackColor = System.Drawing.Color.Transparent;
            this.lblConfirmarSenha.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblConfirmarSenha.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(51)))), ((int)(((byte)(75)))));
            this.lblConfirmarSenha.Location = new System.Drawing.Point(32, 403);
            this.lblConfirmarSenha.Name = "lblConfirmarSenha";
            this.lblConfirmarSenha.Size = new System.Drawing.Size(456, 19);
            this.lblConfirmarSenha.TabIndex = 10;
            this.lblConfirmarSenha.Text = "Confirmar senha *";
            //
            // panelConfirmarSenha
            //
            this.panelConfirmarSenha.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.panelConfirmarSenha.Controls.Add(this.txtConfirmarSenha);
            this.panelConfirmarSenha.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.panelConfirmarSenha.Location = new System.Drawing.Point(32, 426);
            this.panelConfirmarSenha.Name = "panelConfirmarSenha";
            this.panelConfirmarSenha.Padding = new System.Windows.Forms.Padding(12, 2, 12, 2);
            this.panelConfirmarSenha.Size = new System.Drawing.Size(456, 42);
            this.panelConfirmarSenha.TabIndex = 3;
            //
            // txtConfirmarSenha
            //
            this.txtConfirmarSenha.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.txtConfirmarSenha.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtConfirmarSenha.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtConfirmarSenha.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtConfirmarSenha.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(16)))), ((int)(((byte)(43)))), ((int)(((byte)(73)))));
            this.txtConfirmarSenha.Location = new System.Drawing.Point(12, 2);
            this.txtConfirmarSenha.Margin = new System.Windows.Forms.Padding(0);
            this.txtConfirmarSenha.Name = "txtConfirmarSenha";
            this.txtConfirmarSenha.Size = new System.Drawing.Size(432, 18);
            this.txtConfirmarSenha.TabIndex = 0;
            this.txtConfirmarSenha.UseSystemPasswordChar = true;
            //
            // chkMostrarSenha
            //
            this.chkMostrarSenha.AutoSize = true;
            this.chkMostrarSenha.BackColor = System.Drawing.Color.Transparent;
            this.chkMostrarSenha.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.chkMostrarSenha.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(115)))), ((int)(((byte)(132)))), ((int)(((byte)(151)))));
            this.chkMostrarSenha.Location = new System.Drawing.Point(398, 327);
            this.chkMostrarSenha.Name = "chkMostrarSenha";
            this.chkMostrarSenha.Size = new System.Drawing.Size(66, 17);
            this.chkMostrarSenha.TabIndex = 15;
            this.chkMostrarSenha.Text = "Mostrar";
            this.chkMostrarSenha.UseVisualStyleBackColor = false;
            this.chkMostrarSenha.CheckedChanged += new System.EventHandler(this.MostrarSenha_CheckedChanged);
            //
            // lblMensagemCadastro
            //
            this.lblMensagemCadastro.BackColor = System.Drawing.Color.Transparent;
            this.lblMensagemCadastro.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblMensagemCadastro.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(190)))), ((int)(((byte)(55)))), ((int)(((byte)(65)))));
            this.lblMensagemCadastro.Location = new System.Drawing.Point(32, 499);
            this.lblMensagemCadastro.Name = "lblMensagemCadastro";
            this.lblMensagemCadastro.Size = new System.Drawing.Size(456, 38);
            this.lblMensagemCadastro.TabIndex = 12;
            this.lblMensagemCadastro.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblMensagemCadastro.Visible = false;
            //
            // btnCadastrar
            //
            this.btnCadastrar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(105)))), ((int)(((byte)(210)))));
            this.btnCadastrar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCadastrar.FlatAppearance.BorderSize = 0;
            this.btnCadastrar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCadastrar.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnCadastrar.ForeColor = System.Drawing.Color.White;
            this.btnCadastrar.Location = new System.Drawing.Point(32, 543);
            this.btnCadastrar.Name = "btnCadastrar";
            this.btnCadastrar.Size = new System.Drawing.Size(456, 48);
            this.btnCadastrar.TabIndex = 10;
            this.btnCadastrar.Text = "Solicitar cadastro";
            this.btnCadastrar.UseVisualStyleBackColor = false;
            this.btnCadastrar.Click += new System.EventHandler(this.BtnCadastrar_Click);
            //
            // linkVoltarLogin
            //
            this.linkVoltarLogin.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.linkVoltarLogin.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(105)))), ((int)(((byte)(210)))));
            this.linkVoltarLogin.Location = new System.Drawing.Point(32, 602);
            this.linkVoltarLogin.Name = "linkVoltarLogin";
            this.linkVoltarLogin.Size = new System.Drawing.Size(456, 24);
            this.linkVoltarLogin.TabIndex = 11;
            this.linkVoltarLogin.TabStop = true;
            this.linkVoltarLogin.Text = "Já tem uma conta? Voltar ao login";
            this.linkVoltarLogin.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.linkVoltarLogin.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkVoltarLogin_LinkClicked);
            //
            // CadastroForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(16)))), ((int)(((byte)(43)))), ((int)(((byte)(73)))));
            this.BackgroundImage = global::CASE.Properties.Resources.FundoLogin;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(1280, 820);
            this.Controls.Add(this.panelCadastro);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.MinimumSize = new System.Drawing.Size(1040, 780);
            this.Name = "CadastroForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CASE - Criar Conta";
            this.panelCadastro.ResumeLayout(false);
            this.panelCadastro.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureFotoCadastro)).EndInit();
            this.menuFoto.ResumeLayout(false);
            this.panelNomeCompleto.ResumeLayout(false);
            this.panelNomeCompleto.PerformLayout();
            this.panelUsuarioCadastro.ResumeLayout(false);
            this.panelUsuarioCadastro.PerformLayout();
            this.panelSenhaCadastro.ResumeLayout(false);
            this.panelSenhaCadastro.PerformLayout();
            this.panelConfirmarSenha.ResumeLayout(false);
            this.panelConfirmarSenha.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.Panel panelCadastro;
        private System.Windows.Forms.Label lblTituloCadastro;
        private System.Windows.Forms.Label lblSubtituloCadastro;
        private System.Windows.Forms.Label lblFotoOpcional;
        private System.Windows.Forms.PictureBox pictureFotoCadastro;
        private System.Windows.Forms.Button btnSelecionarFoto;
        private System.Windows.Forms.Label lblNomeCompleto;
        private System.Windows.Forms.Panel panelNomeCompleto;
        private System.Windows.Forms.TextBox txtNomeCompleto;
        private System.Windows.Forms.Label lblUsuario;
        private System.Windows.Forms.Panel panelUsuarioCadastro;
        private System.Windows.Forms.TextBox txtUsuario;
        private System.Windows.Forms.Label lblSenha;
        private System.Windows.Forms.Panel panelSenhaCadastro;
        private System.Windows.Forms.TextBox txtSenha;
        private System.Windows.Forms.Label lblConfirmarSenha;
        private System.Windows.Forms.Panel panelConfirmarSenha;
        private System.Windows.Forms.TextBox txtConfirmarSenha;
        private System.Windows.Forms.CheckBox chkMostrarSenha;
        private System.Windows.Forms.Label lblMensagemCadastro;
        private System.Windows.Forms.Button btnCadastrar;
        private System.Windows.Forms.LinkLabel linkVoltarLogin;
        private System.Windows.Forms.ContextMenuStrip menuFoto;
        private System.Windows.Forms.ToolStripMenuItem itemRemoverFoto;
    }
}
