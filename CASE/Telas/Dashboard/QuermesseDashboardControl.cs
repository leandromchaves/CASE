using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE.Quermesse
{
    public partial class QuermesseDashboardControl : UserControl
    {
        private readonly Button btnGerarPdf = new Button();
        private readonly Button btnGerarExcel = new Button();
        private DataTable dadosAtividade;

        public QuermesseDashboardControl() : this(null, null)
        {
        }

        public QuermesseDashboardControl(Usuario usuario, Evento evento)
        {
            InitializeComponent();
            FrontendTheme.Aplicar(this);
            ConfigurarExportacoes();
            lblValorIndicador0.Text = lblValorIndicador1.Text = "—";
            lblValorIndicador2.Text = lblValorIndicador3.Text = "—";
            lblPerfilPreview.Text = "Resumo do evento";
            lblDescricaoPerfil.Text = "Dados limitados às permissões da conta autenticada.";
            AjustarIndicadores();
            if (usuario == null || evento == null)
            {
                lblSubtitulo.Text = "Selecione um evento autorizado.";
                labelAviso.Text = "O resumo requer uma sessão autenticada e um evento selecionado.";
                return;
            }

            try
            {
                Evento autorizado = new EventoService().BuscarPorId(usuario, evento.IdEvento);
                if (autorizado == null)
                {
                    throw new InvalidOperationException("O evento não foi encontrado.");
                }
                lblSubtitulo.Text = autorizado.Nome;
                DataTable registros = new ConsultaOperacionalService().Registros(usuario,
                    autorizado.IdEvento, autorizado.DataInicio, autorizado.DataFim);
                HashSet<int> operacoes = new HashSet<int>();
                decimal itensVendidos = 0;
                foreach (DataRow registro in registros.Rows)
                {
                    operacoes.Add(Convert.ToInt32(registro["id_registro"]));
                    if (Convert.ToString(registro["tipo"]) == TiposRegistro.Venda)
                    {
                        itensVendidos += Convert.ToDecimal(registro["quantidade"]);
                    }
                    dgvAtividade.Rows.Add(registro["responsavel"], registro["setor"],
                        registro["turno"], registro["tipo"], registro["produto"], registro["quantidade"]);
                }
                lblValorIndicador0.Text = operacoes.Count.ToString();
                lblValorIndicador1.Text = itensVendidos.ToString("N0");
                lblValorIndicador2.Text = new EquipeEventoService().ListarPorEvento(usuario, autorizado.IdEvento).Count.ToString();
                lblValorIndicador3.Text = new SetorService().ListarPorEvento(usuario, autorizado.IdEvento).Count.ToString();
                dadosAtividade = registros.Copy();
                btnGerarPdf.Enabled = btnGerarExcel.Enabled = true;
                VisualUtils.ExibirEstadoVazio(dgvAtividade, "Nenhuma atividade autorizada encontrada.");
                labelAviso.Text = string.Empty;
            }
            catch (Exception erro)
            {
                labelAviso.Text = "Não foi possível carregar o resumo autorizado do evento.";
                System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em ResumoEvento: " + erro.StackTrace);
            }
        }

        private void ConfigurarExportacoes()
        {
            btnGerarPdf.Name = "btnGerarResumoPdf";
            btnGerarPdf.Text = "Gerar PDF";
            btnGerarPdf.Size = new System.Drawing.Size(104, 34);
            btnGerarPdf.Enabled = false;
            btnGerarPdf.CausesValidation = false;
            btnGerarPdf.Click += (s, e) => ExportarResumo("pdf");

            btnGerarExcel.Name = "btnGerarResumoExcel";
            btnGerarExcel.Text = "Gerar Excel";
            btnGerarExcel.Size = new System.Drawing.Size(112, 34);
            btnGerarExcel.Enabled = false;
            btnGerarExcel.CausesValidation = false;
            btnGerarExcel.Click += (s, e) => ExportarResumo("xlsx");

            foreach (Button botao in new[] { btnGerarPdf, btnGerarExcel })
            {
                botao.FlatStyle = FlatStyle.Flat;
                botao.BackColor = System.Drawing.Color.White;
                botao.ForeColor = VisualUtils.Texto;
                VisualUtils.EstilizarBotao(botao);
                VisualUtils.Arredondar(botao, 8);
                panelAtividade.Controls.Add(botao);
            }

            panelAtividade.SizeChanged += (s, e) => OrganizarExportacoes();
            OrganizarExportacoes();
        }

        private void OrganizarExportacoes()
        {
            int largura = panelAtividade.ClientSize.Width;
            int margem = 18;
            btnGerarExcel.SetBounds(largura - margem - btnGerarExcel.Width, 11,
                btnGerarExcel.Width, 34);
            btnGerarPdf.SetBounds(btnGerarExcel.Left - 8 - btnGerarPdf.Width, 11,
                btnGerarPdf.Width, 34);
            lblAtividaderecente.SetBounds(margem, 14,
                Math.Max(120, btnGerarPdf.Left - margem - 12), 28);
        }

        private void ExportarResumo(string formato)
        {
            if (dadosAtividade == null)
                return;

            using (var dialogo = new SaveFileDialog())
            {
                dialogo.Filter = formato == "pdf"
                    ? "Documento PDF (*.pdf)|*.pdf"
                    : "Planilha Excel (*.xlsx)|*.xlsx";
                dialogo.DefaultExt = formato;
                dialogo.AddExtension = true;
                dialogo.FileName = "CASE_resumo_evento_" + DateTime.Today.ToString("yyyyMMdd") + "." + formato;
                if (dialogo.ShowDialog(this) != DialogResult.OK)
                    return;

                btnGerarPdf.Enabled = btnGerarExcel.Enabled = false;
                UseWaitCursor = true;
                try
                {
                    ContextoRelatorio contexto = new ContextoRelatorio
                    {
                        Titulo = "Resumo do evento — " + lblSubtitulo.Text,
                        Filtros = "Evento: " + lblSubtitulo.Text +
                            " | Registros: " + lblValorIndicador0.Text +
                            " | Itens vendidos: " + lblValorIndicador1.Text +
                            " | Pessoas na equipe: " + lblValorIndicador2.Text +
                            " | Setores: " + lblValorIndicador3.Text
                    };

                    if (formato == "pdf")
                        ExportadorRelatorio.ExportarPdf(dadosAtividade, dialogo.FileName, contexto);
                    else
                        ExportadorRelatorio.ExportarExcel(dadosAtividade, dialogo.FileName, contexto);

                    labelAviso.Text = "Resumo do evento exportado para " + formato.ToUpperInvariant() + ".";
                    FeedbackUsuario.Exibir(this, labelAviso.Text, TipoFeedback.Sucesso);
                }
                catch (Exception erro)
                {
                    FeedbackUsuario.ExibirErro(this, erro,
                        "Não foi possível gerar o arquivo. Confira o local escolhido e tente novamente.", labelAviso);
                    System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em ExportarResumoEvento: " + erro.StackTrace);
                }
                finally
                {
                    UseWaitCursor = false;
                    btnGerarPdf.Enabled = btnGerarExcel.Enabled = dadosAtividade != null;
                }
            }
        }

        private void panelCorpo_SizeChanged(object sender, EventArgs e)
        {
            AjustarIndicadores();
        }

        private void AjustarIndicadores()
        {
            Panel[] indicadores = { panelIndicador0, panelIndicador1, panelIndicador2, panelIndicador3 };
            for (int indice = 0; indice < indicadores.Length; indice++)
            {
                indicadores[indice].SetBounds(indice * (panelCorpo.Width + 10) / 4,
                    0, (panelCorpo.Width - 30) / 4, 105);
            }
        }
    }
}
