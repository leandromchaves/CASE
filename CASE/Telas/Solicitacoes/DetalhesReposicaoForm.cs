using System;
using System.Drawing;
using System.Windows.Forms;
using CASE.Models;
using CASE.Services;
using CASE.Utils;

namespace CASE.Quermesse
{
    public sealed partial class DetalhesReposicaoForm : Form
    {
        public DetalhesReposicaoForm()
        {
            InitializeComponent();
        }

        public DetalhesReposicaoForm(Usuario usuario, int id, bool analisar) : this()
        {
            var service = new SolicitacaoService();
            Solicitacao pedido = service.BuscarPorId(usuario, id);
            var resumo = service.ObterResumo(usuario, id);
            Text = lblTitulo.Text = "Reposição do setor #" + id;
            lblSituacao.Text = TextoInterface.FormatarValorBanco(pedido.Situacao);
            lblResumo.Text = resumo.Evento + " · " + resumo.Setor + Environment.NewLine + "Solicitante: " + resumo.Solicitante +
                Environment.NewLine + "Solicitado em " + pedido.DataHora.ToString("dd/MM/yyyy HH:mm");
            foreach (var item in resumo.Itens) grade.Rows.Add(item.Produto, item.Quantidade.ToString("0.###"), item.Unidade);
            string orientacao = pedido.Situacao == SituacoesSolicitacao.Pendente
                ? "Aprovar autoriza o pedido. Os produtos só saem do estoque geral ao efetivar a transferência."
                : pedido.Situacao == SituacoesSolicitacao.Aprovada
                    ? "Pronto para atender: a transferência retira os produtos do estoque geral e abastece o setor."
                    : "Consulte os produtos e a situação do pedido.";
            txtObservacoes.Text = orientacao + Environment.NewLine + (string.IsNullOrWhiteSpace(pedido.Descricao) ? "" : "Observação: " + pedido.Descricao) +
                (string.IsNullOrWhiteSpace(pedido.JustificativaRejeicao) ? "" : Environment.NewLine + "Motivo da rejeição: " + pedido.JustificativaRejeicao);
            bool podeDecidir = analisar && (pedido.Situacao == SituacoesSolicitacao.Pendente || pedido.Situacao == SituacoesSolicitacao.Aprovada);
            btnAprovar.Visible = podeDecidir;
            btnAprovar.Text = pedido.Situacao == SituacoesSolicitacao.Aprovada ? "Efetivar transferência" : "Aprovar pedido";
            btnRejeitar.Visible = podeDecidir && pedido.Situacao == SituacoesSolicitacao.Pendente;
            IconoAplicacao.Aplicar(this);
            VisualUtils.Arredondar(lblSituacao, 8);
            FrontendTheme.Aplicar(this);
            VisualUtils.ExibirEstadoVazio(grade, "Nenhum item encontrado neste pedido.");
        }
    }
}
