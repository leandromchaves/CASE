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
        public QuermesseDashboardControl() : this(null, null)
        {
        }

        public QuermesseDashboardControl(Usuario usuario, Evento evento)
        {
            InitializeComponent();
            FrontendTheme.Aplicar(this);
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
                VisualUtils.ExibirEstadoVazio(dgvAtividade, "Nenhuma atividade autorizada encontrada.");
                labelAviso.Text = string.Empty;
            }
            catch (Exception erro)
            {
                labelAviso.Text = "Não foi possível carregar o resumo autorizado do evento.";
                System.Diagnostics.Trace.WriteLine(erro.GetType().Name + " em ResumoEvento: " + erro.StackTrace);
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
