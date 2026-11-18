using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;

namespace CASE.Utils
{
    public sealed class ResumoGraficoRelatorio
    {
        public string Titulo { get; set; }
        public bool Monetario { get; set; }
        public DataTable Valores { get; } = new DataTable();

        public ResumoGraficoRelatorio()
        {
            Valores.Columns.Add("Grupo", typeof(string));
            Valores.Columns.Add("Valor", typeof(decimal));
        }
    }

    public static class GraficoRelatorio
    {
        public static string[] TiposDisponiveis(string relatorio)
        {
            return relatorio == "Estoque"
                ? new[] { "Colunas", "Barras", "Pizza", "Rosca" }
                : new[] { "Colunas", "Barras", "Linhas", "Área", "Pizza", "Rosca" };
        }

        public static ResumoGraficoRelatorio Resumir(DataTable dados, string relatorio, string formato)
        {
            if (!TiposDisponiveis(relatorio).Contains(formato))
                throw new ArgumentException("Selecione um tipo de gráfico disponível para este relatório.");
            var resumo = new ResumoGraficoRelatorio();
            if (dados == null)
                return resumo;
            string grupo;
            string valor = null;
            string identificador = null;
            string data = "data_hora";
            var linhas = dados.Rows.Cast<DataRow>();
            switch (relatorio)
            {
                case "Movimentações":
                    grupo = "tipo"; valor = "quantidade";
                    resumo.Titulo = "Unidades movimentadas";
                    break;
                case "Estoque":
                    grupo = "produto"; valor = "quantidade_atual";
                    resumo.Titulo = "Saldo por produto (unidade cadastrada)";
                    break;
                case "Vendas e perdas":
                    grupo = "forma_pagamento"; valor = "total";
                    linhas = linhas.Where(r => Convert.ToString(r["tipo"]) == "venda");
                    resumo.Titulo = "Vendas por pagamento (R$)";
                    resumo.Monetario = true;
                    break;
                case "Despesas":
                    grupo = "evento"; valor = "valor"; data = "data";
                    resumo.Titulo = "Despesas por evento (R$)";
                    resumo.Monetario = true;
                    break;
                case "Eventos":
                    grupo = "situacao"; data = "data_inicio";
                    resumo.Titulo = "Eventos por situação";
                    break;
                case "Solicitações de reposição":
                    grupo = "situacao"; identificador = "id_solicitacao";
                    resumo.Titulo = "Pedidos de reposição por situação";
                    break;
                case "Solicitações ao central":
                    grupo = "situacao"; identificador = "id_solicitacao_estoque_central";
                    resumo.Titulo = "Pedidos ao central por situação";
                    break;
                case "Apurações":
                    // Reconferências são contadas, sem somar seus valores como receitas.
                    grupo = "setor"; identificador = "id_apuracao";
                    resumo.Titulo = "Quantidade de conferências por setor";
                    break;
                default:
                    throw new ArgumentException("Este relatório não possui gráfico configurado.");
            }
            bool temporal = formato == "Linhas" || formato == "Área";
            if (temporal)
            {
                grupo = data;
                resumo.Titulo = resumo.Monetario ? "Valores por dia (R$)" :
                    valor == "quantidade" ? "Unidades movimentadas por dia" : "Quantidade de registros por dia";
            }
            if (identificador != null)
                linhas = linhas.GroupBy(r => r[identificador]).Select(g => g.First());
            if (valor != null)
                linhas = linhas.Where(r => r[valor] != DBNull.Value);
            if (temporal)
                linhas = linhas.Where(r => r[grupo] != DBNull.Value);
            var grupos = linhas.GroupBy(r => temporal
                ? Convert.ToDateTime(r[grupo]).ToString("yyyy-MM-dd")
                : Convert.ToString(r[grupo]));
            var totais = grupos.Select(g => new
            {
                Nome = g.Key,
                Valor = valor == null ? g.Count() : g.Sum(r => Convert.ToDecimal(r[valor]))
            });
            if (temporal)
                totais = totais.OrderBy(g => g.Nome);
            else
                totais = totais.OrderByDescending(g => g.Valor);
            var lista = totais.ToList();
            // O agrupamento reduz legendas longas e preserva a soma integral.
            int limite = temporal ? lista.Count : 7;
            foreach (var item in lista.Take(limite))
                resumo.Valores.Rows.Add(temporal ? DateTime.ParseExact(item.Nome, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture).ToString("dd/MM/yyyy") : NomeGrupo(item.Nome), item.Valor);
            if (lista.Count > limite)
                resumo.Valores.Rows.Add("Outros", lista.Skip(limite).Sum(g => g.Valor));
            return resumo;
        }

        private static string NomeGrupo(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return "Não informado";
            switch (nome)
            {
                case "pix": return "Pix";
                case "cartao": return "Cartão";
                case "dinheiro": return "Dinheiro";
                case "transferencia": return "Transferência";
                case "entrada": return "Entrada";
                case "venda": return "Venda";
                case "perda": return "Perda";
                default: return nome.Replace('_', ' ');
            }
        }

        public static Chart Criar(DataTable dados, string relatorio, string formato)
        {
            ResumoGraficoRelatorio resumo = Resumir(dados, relatorio, formato);
            var grafico = new Chart { BackColor = Color.White, Width = 1000, Height = 620 };
            var area = new ChartArea("Resumo");
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisY.MajorGrid.LineColor = VisualUtils.Borda;
            area.AxisX.Interval = 1;
            area.AxisX.LabelStyle.Font = area.AxisY.LabelStyle.Font = new Font("Segoe UI", 10F);
            area.AxisX.LabelStyle.ForeColor = area.AxisY.LabelStyle.ForeColor = VisualUtils.TextoSuave;
            area.AxisX.LineColor = area.AxisY.LineColor = VisualUtils.Borda;
            area.AxisY.Minimum = 0;
            area.AxisY.LabelStyle.Format = resumo.Monetario ? "C0" : "0.###";
            grafico.ChartAreas.Add(area);
            string[] nomes = { "Colunas", "Barras", "Linhas", "Área", "Pizza", "Rosca" };
            SeriesChartType[] tipos = { SeriesChartType.Column, SeriesChartType.Bar, SeriesChartType.Line,
                SeriesChartType.Area, SeriesChartType.Pie, SeriesChartType.Doughnut };
            var serie = new Series("Resumo")
            {
                ChartType = tipos[Array.IndexOf(nomes, formato)], BorderWidth = 3,
                Color = VisualUtils.Azul, Font = new Font("Segoe UI", 10F),
                ToolTip = "#AXISLABEL: #VALY{0.###}", IsXValueIndexed = true
            };
            bool circular = formato == "Pizza" || formato == "Rosca";
            serie.IsVisibleInLegend = circular;
            if (circular)
            {
                serie.Color = Color.Empty;
                serie.BorderColor = Color.White;
                serie.Label = "#PERCENT{P0}";
                serie.LabelForeColor = Color.White;
                serie.LegendText = "#AXISLABEL";
                serie["PieLabelStyle"] = "Inside";
                serie["DoughnutRadius"] = "65";
                area.AxisX.Enabled = area.AxisY.Enabled = AxisEnabled.False;
                grafico.Legends.Add(new Legend("Grupos")
                {
                    Docking = Docking.Bottom, Font = new Font("Segoe UI", 10F),
                    MaximumAutoSize = 30, Alignment = StringAlignment.Center
                });
            }
            if (formato == "Linhas")
            {
                serie.MarkerStyle = MarkerStyle.Circle;
                serie.MarkerSize = 8;
            }
            if (formato == "Área")
            {
                serie.BackSecondaryColor = Color.FromArgb(230, 240, 255);
                serie.BackGradientStyle = GradientStyle.TopBottom;
            }
            foreach (DataRow linha in resumo.Valores.Rows)
                serie.Points.AddXY(linha["Grupo"], linha["Valor"]);
            if (circular)
            {
                double total = serie.Points.Sum(p => p.YValues[0]);
                foreach (DataPoint ponto in serie.Points)
                    if (total > 0 && ponto.YValues[0] / total < 0.03)
                        ponto.Label = " ";
            }
            if (formato == "Linhas" && serie.Points.Count == 1)
            {
                serie.IsValueShownAsLabel = true;
                grafico.Titles.Add(new Title("Há dados em apenas um dia; a linha precisa de dois dias.")
                    { Docking = Docking.Bottom, Font = new Font("Segoe UI", 9F), ForeColor = VisualUtils.TextoSuave });
            }
            grafico.Series.Add(serie);
            grafico.Palette = ChartColorPalette.None;
            grafico.PaletteCustomColors = new[] { VisualUtils.Azul, Color.Teal, Color.Goldenrod,
                Color.MediumPurple, Color.SteelBlue, Color.IndianRed, Color.DarkSeaGreen, Color.Gray };
            grafico.Titles.Add(new Title(resumo.Titulo) { Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = VisualUtils.Marinho });
            if (resumo.Valores.Rows.Count == 0 || resumo.Valores.Rows.Cast<DataRow>().All(r => Convert.ToDecimal(r["Valor"]) == 0))
            {
                serie.Points.Clear();
                grafico.Titles.Add("Sem valores para representar neste gráfico.");
            }
            grafico.AntiAliasing = AntiAliasingStyles.All;
            return grafico;
        }

        public static byte[] CriarImagem(DataTable dados, string relatorio, string formato)
        {
            using (Chart grafico = Criar(dados, relatorio, formato))
            using (var memoria = new MemoryStream())
            {
                // A imagem é reduzida no documento; as fontes maiores mantêm a leitura.
                foreach (Title titulo in grafico.Titles)
                    titulo.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
                grafico.Series[0].Font = new Font("Segoe UI", 20F);
                foreach (Legend legenda in grafico.Legends)
                    legenda.Font = new Font("Segoe UI", 17F);
                var area = grafico.ChartAreas[0];
                area.AxisX.LabelStyle.Font = area.AxisY.LabelStyle.Font = new Font("Segoe UI", 17F);
                area.AxisX.LabelStyle.Interval = Math.Max(1, (int)Math.Ceiling(grafico.Series[0].Points.Count / 6.0));
                grafico.SaveImage(memoria, ChartImageFormat.Png);
                return memoria.ToArray();
            }
        }
    }
}
