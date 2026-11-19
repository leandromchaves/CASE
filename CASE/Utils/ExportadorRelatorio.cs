using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

namespace CASE.Utils
{
    public sealed class ContextoRelatorio
    {
        public string Titulo { get; set; } = "Relatório";
        public string Instituicao { get; set; }
        public string TipoGrafico { get; set; }
        public string Filtros { get; set; } = "Dados autorizados da consulta";
        public DateTime Emissao { get; set; } = DateTime.Now;
    }

    public static class ExportadorRelatorio
    {
        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

        private static DataTable Preparar(DataTable dados, string caminho)
        {
            if (dados == null || dados.Columns.Count == 0 || string.IsNullOrWhiteSpace(caminho))
                throw new ArgumentException("Selecione dados e um arquivo de destino.");
            string[] colunas = dados.Columns.Cast<DataColumn>().Where(c =>
                !c.ColumnName.StartsWith("id_", StringComparison.OrdinalIgnoreCase) &&
                c.ColumnName != "entidade" &&
                c.ColumnName.IndexOf("hash", StringComparison.OrdinalIgnoreCase) < 0 &&
                c.ColumnName.IndexOf("senha", StringComparison.OrdinalIgnoreCase) < 0 &&
                c.DataType != typeof(byte[])).Select(c => c.ColumnName).ToArray();
            if (colunas.Length == 0)
                throw new ArgumentException("Não há colunas publicáveis no relatório.");
            return dados.DefaultView.ToTable(false, colunas);
        }

        private static string Cabecalho(DataColumn coluna)
        {
            switch (coluna.ColumnName)
            {
                case "responsavel":
                    return "Responsável";
                case "observacao":
                    return "Observação";
                case "descricao":
                    return "Descrição";
                case "data_hora":
                    return "Data / hora";
                case "preco_unitario_praticado":
                    return "Preço praticado";
                case "forma_pagamento":
                    return "Pagamento";
                case "reposicao_minima":
                    return "Falta para o mínimo";
            }
            return Cultura.TextInfo.ToTitleCase(coluna.ColumnName.Replace('_', ' '));
        }

        private static bool Monetaria(DataColumn c)
        {
            string n = c.ColumnName;
            return n == "valor" || n == "total" || n == "esperado_atual" ||
                n.StartsWith("valor_") || n.StartsWith("preco_") || n.StartsWith("custo_") ||
                n.StartsWith("diferenca_");
        }

        private static string Formatar(object valor, DataColumn coluna)
        {
            if (valor == DBNull.Value)
                return string.Empty;
            if (valor is DateTime)
                return ((DateTime)valor).ToString(coluna.ColumnName.Contains("hora") ? "dd/MM/yyyy HH:mm" : "dd/MM/yyyy", Cultura);
            if (Monetaria(coluna) && valor is IConvertible)
                return Convert.ToDecimal(valor, Cultura).ToString("C2", Cultura);
            return Convert.ToString(valor, Cultura);
        }

        // Preços unitários, saldos e conferências sucessivas não são receitas somáveis.
        private static bool Totalizavel(DataTable dados, DataColumn c, ContextoRelatorio contexto)
        {
            return (c.ColumnName == "valor" || c.ColumnName == "total") &&
                contexto.Titulo != "Apurações" && c.DataType != typeof(string);
        }

        private static decimal Somar(DataTable dados, DataColumn c)
        {
            return dados.Rows.Cast<DataRow>().Where(r => r[c] != DBNull.Value &&
                (!dados.Columns.Contains("tipo") || Convert.ToString(r["tipo"]) != "perda"))
                .Sum(r => Convert.ToDecimal(r[c], Cultura));
        }

        public static void ExportarCsv(DataTable dados, string caminho)
        {
            DataTable tabela = Preparar(dados, caminho);
            using (var escritor = new StreamWriter(caminho, false, new UTF8Encoding(true)))
            {
                escritor.WriteLine(string.Join(";", tabela.Columns.Cast<DataColumn>().Select(c => CampoCsv(Cabecalho(c)))));
                foreach (DataRow linha in tabela.Rows)
                    escritor.WriteLine(string.Join(";", tabela.Columns.Cast<DataColumn>().Select(c => CampoCsv(Formatar(linha[c], c)))));
            }
        }

        private static string CampoCsv(string valor)
        {
            string inicio = valor.TrimStart();
            if (inicio.StartsWith("=") || inicio.StartsWith("+") || inicio.StartsWith("-") ||
                inicio.StartsWith("@") || valor.StartsWith("\t") || valor.StartsWith("\r"))
                valor = "'" + valor;
            return "\"" + valor.Replace("\"", "\"\"") + "\"";
        }

        public static void ExportarExcel(DataTable dados, string caminho, ContextoRelatorio contexto)
        {
            DataTable tabela = Preparar(dados, caminho);
            contexto = contexto ?? new ContextoRelatorio();
            using (var livro = new XLWorkbook())
            {
                var folha = livro.Worksheets.Add("Relatório");
                int quantidade = tabela.Columns.Count;
                folha.Range(1, 1, 1, quantidade).Merge().Value = "CASE — " + contexto.Titulo;
                folha.Row(1).Style.Font.Bold = true;
                folha.Row(1).Style.Font.FontSize = 18;
                folha.Range(2, 1, 2, quantidade).Merge().Value = contexto.Instituicao ?? "CASE";
                folha.Range(3, 1, 3, quantidade).Merge().Value = "Emitido em " + contexto.Emissao.ToString("dd/MM/yyyy HH:mm", Cultura);
                folha.Range(4, 1, 4, quantidade).Merge().Value = contexto.Filtros;
                folha.Row(4).Style.Alignment.WrapText = true;
                folha.Row(4).Height = 32;
                for (int c = 0; c < quantidade; c++)
                {
                    DataColumn coluna = tabela.Columns[c];
                    folha.Cell(6, c + 1).Value = Cabecalho(coluna);
                    for (int r = 0; r < tabela.Rows.Count; r++)
                    {
                        object valor = tabela.Rows[r][c];
                        var celula = folha.Cell(r + 7, c + 1);
                        if (valor == DBNull.Value)
                            continue;
                        if (valor is DateTime)
                            celula.Value = (DateTime)valor;
                        else if (valor is decimal || valor is double || valor is float || valor is int || valor is long)
                            celula.Value = Convert.ToDouble(valor, Cultura);
                        else
                            celula.Value = Convert.ToString(valor, Cultura);
                    }
                    folha.Column(c + 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                    if (Monetaria(coluna))
                        folha.Column(c + 1).Style.NumberFormat.Format = "\"R$\" #,##0.00;[Red]-\"R$\" #,##0.00";
                    else if (coluna.DataType == typeof(DateTime))
                        folha.Column(c + 1).Style.NumberFormat.Format =
                        coluna.ColumnName.Contains("hora") ? "dd/mm/yyyy hh:mm" : "dd/mm/yyyy";
                }
                var cabecalho = folha.Range(6, 1, 6, quantidade);
                cabecalho.Style.Fill.BackgroundColor = XLColor.FromHtml("#1764C0");
                cabecalho.Style.Font.FontColor = XLColor.White;
                cabecalho.Style.Font.Bold = true;
                if (tabela.Rows.Count > 0)
                {
                    var intervalo = folha.Range(6, 1, tabela.Rows.Count + 6, quantidade);
                    intervalo.SetAutoFilter();
                    intervalo.Style.Border.BottomBorder = XLBorderStyleValues.Hair;
                    intervalo.Style.Border.BottomBorderColor = XLColor.LightGray;
                }
                folha.SheetView.FreezeRows(6);
                folha.Columns(1, quantidade).AdjustToContents(6, Math.Min(tabela.Rows.Count + 6, 206));
                foreach (var coluna in folha.Columns(1, quantidade))
                {
                    coluna.Width = Math.Max(15, Math.Min(45, coluna.Width + 2));
                    coluna.Style.Alignment.WrapText = true;
                }
                if (tabela.Rows.Count > 0)
                    folha.Rows(7, tabela.Rows.Count + 6).AdjustToContents();
                int rodape = tabela.Rows.Count + 8;
                folha.Cell(rodape, 1).Value = tabela.Rows.Count + " registro(s)";
                foreach (DataColumn c in tabela.Columns)
                    if (Totalizavel(tabela, c, contexto))
                    {
                        folha.Cell(rodape + 1, c.Ordinal + 1).Value = Convert.ToDouble(Somar(tabela, c));
                        folha.Cell(rodape + 1, c.Ordinal + 1).Style.Font.Bold = true;
                        if (c.Ordinal > 0)
                            folha.Cell(rodape + 1, 1).Value = "Total";
                    }
                if (tabela.Rows.Count == 0)
                    folha.Cell(7, 1).Value = "Nenhum registro encontrado.";
                if (!string.IsNullOrWhiteSpace(contexto.TipoGrafico))
                {
                    var resumo = GraficoRelatorio.Resumir(dados, contexto.Titulo, contexto.TipoGrafico);
                    int inicioResumo = quantidade + 2;
                    folha.Cell(5, inicioResumo).Value = resumo.Titulo;
                    folha.Cell(5, inicioResumo).Style.Font.Bold = true;
                    using (var imagem = new MemoryStream(GraficoRelatorio.CriarImagem(dados, contexto.Titulo, contexto.TipoGrafico)))
                        folha.AddPicture(imagem).MoveTo(folha.Cell(6, inicioResumo)).WithSize(600, 372);
                    folha.Cell(30, inicioResumo).Value = "Grupo";
                    folha.Cell(30, inicioResumo + 1).Value = resumo.Monetario ? "Valor (R$)" : "Quantidade";
                    folha.Range(30, inicioResumo, 30, inicioResumo + 1).Style.Font.Bold = true;
                    for (int i = 0; i < resumo.Valores.Rows.Count; i++)
                    {
                        folha.Cell(31 + i, inicioResumo).Value = Convert.ToString(resumo.Valores.Rows[i][0]);
                        folha.Cell(31 + i, inicioResumo + 1).Value = Convert.ToDouble(resumo.Valores.Rows[i][1]);
                        folha.Cell(31 + i, inicioResumo + 1).Style.NumberFormat.Format =
                            resumo.Monetario ? "\"R$\" #,##0.00" : "0.###";
                    }
                    folha.Column(inicioResumo).Width = 45;
                    folha.Column(inicioResumo + 1).Width = 20;
                }
                livro.SaveAs(caminho);
            }
        }

        public static void ExportarPdf(DataTable dados, string caminho)
        {
            ExportarPdf(dados, caminho, new ContextoRelatorio());
        }

        public static void ExportarPdf(DataTable dados, string caminho, ContextoRelatorio contexto)
        {
            DataTable tabela = Preparar(dados, caminho);
            contexto = contexto ?? new ContextoRelatorio();
            var documento = new Document();
            documento.Info.Title = contexto.Titulo;
            documento.Info.Author = "CASE";
            documento.Styles["Normal"].Font.Name = "Arial";
            documento.Styles["Normal"].Font.Size = 8.5;
            documento.Styles["Normal"].Font.Color = Color.FromRgb(31, 49, 68);
            var secao = documento.AddSection();
            bool muitoLargo = tabela.Columns.Count > 12;
            secao.PageSetup.PageFormat = muitoLargo ? PageFormat.A3 : PageFormat.A4;
            bool paisagem = tabela.Columns.Count > 6 || !string.IsNullOrWhiteSpace(contexto.TipoGrafico);
            secao.PageSetup.Orientation = paisagem ? Orientation.Landscape : Orientation.Portrait;
            secao.PageSetup.LeftMargin = secao.PageSetup.RightMargin = Unit.FromCentimeter(1.3);
            secao.PageSetup.TopMargin = Unit.FromCentimeter(1.7);
            secao.PageSetup.BottomMargin = Unit.FromCentimeter(1.7);
            secao.PageSetup.FooterDistance = Unit.FromCentimeter(0.7);
            secao.PageSetup.HeaderDistance = Unit.FromCentimeter(0.8);
            var identidade = secao.Headers.Primary.AddParagraph("CASE  /  GESTÃO DE ESTOQUE E EVENTOS");
            identidade.Format.Font.Bold = true;
            identidade.Format.Font.Size = 9;
            identidade.Format.Font.Color = Color.FromRgb(23, 100, 192);
            identidade.Format.Borders.Bottom.Color = Color.FromRgb(23, 100, 192);
            identidade.Format.Borders.Bottom.Width = 1.2;
            identidade.Format.Borders.DistanceFromBottom = Unit.FromMillimeter(2);
            if (!string.IsNullOrWhiteSpace(contexto.Instituicao))
            {
                var instituicao = secao.AddParagraph(contexto.Instituicao);
                instituicao.Format.Font.Size = 9;
                instituicao.Format.Font.Color = Color.FromRgb(91, 111, 132);
                instituicao.Format.SpaceAfter = Unit.FromMillimeter(1);
                instituicao.Format.KeepWithNext = true;
            }
            var titulo = secao.AddParagraph(contexto.Titulo);
            titulo.Format.Font.Size = 20;
            titulo.Format.Font.Bold = true;
            titulo.Format.SpaceAfter = Unit.FromCentimeter(0.2);
            titulo.Format.KeepWithNext = true;
            var emissao = secao.AddParagraph("Emitido em " + contexto.Emissao.ToString("dd/MM/yyyy 'às' HH:mm", Cultura));
            emissao.Format.Font.Color = Color.FromRgb(91, 111, 132);
            emissao.Format.SpaceAfter = Unit.FromMillimeter(1.5);
            emissao.Format.KeepWithNext = true;
            var filtros = secao.AddParagraph(contexto.Filtros);
            filtros.Format.Font.Color = Color.FromRgb(91, 111, 132);
            filtros.Format.SpaceAfter = Unit.FromCentimeter(0.3);
            filtros.Format.KeepWithNext = true;
            double largura = (muitoLargo ? 42 : paisagem ? 29.7 : 21) - 2.6;
            if (!string.IsNullOrWhiteSpace(contexto.TipoGrafico))
                AdicionarGraficoPdf(secao, dados, contexto, largura);
            var rodape = secao.Footers.Primary.AddParagraph("CASE  |  Página ");
            rodape.AddPageField();
            rodape.AddText(" de ");
            rodape.AddNumPagesField();
            rodape.Format.Alignment = ParagraphAlignment.Right;
            rodape.Format.Font.Size = 8;
            rodape.Format.Font.Color = Color.FromRgb(91, 111, 132);
            rodape.Format.Borders.Top.Color = Color.FromRgb(220, 228, 237);
            rodape.Format.Borders.Top.Width = 0.5;
            rodape.Format.Borders.DistanceFromTop = Unit.FromMillimeter(2);
            var quantidade = secao.AddParagraph(tabela.Rows.Count + " registro(s) no relatório.");
            quantidade.Format.SpaceAfter = Unit.FromMillimeter(2);
            quantidade.Format.Font.Color = Color.FromRgb(91, 111, 132);
            quantidade.Format.KeepWithNext = true;
            foreach (DataColumn c in tabela.Columns)
                if (Totalizavel(tabela, c, contexto))
                {
                    var total = secao.AddParagraph("Total — " + Cabecalho(c) + ": " + Somar(tabela, c).ToString("C2", Cultura));
                    total.Format.Font.Bold = true;
                    total.Format.SpaceAfter = Unit.FromMillimeter(2);
                    total.Format.KeepWithNext = true;
                }
            var grade = secao.AddTable();
            grade.Borders.Color = Color.FromRgb(220, 228, 237);
            grade.Borders.Width = 0;
            grade.Borders.Bottom.Width = 0.4;
            grade.TopPadding = grade.BottomPadding = Unit.FromMillimeter(1);
            grade.LeftPadding = grade.RightPadding = Unit.FromMillimeter(1.5);
            double[] pesos = tabela.Columns.Cast<DataColumn>().Select(PesoColunaPdf).ToArray();
            for (int c = 0; c < tabela.Columns.Count; c++)
                grade.AddColumn(Unit.FromCentimeter(largura * pesos[c] / pesos.Sum()));
            var header = grade.AddRow();
            header.HeadingFormat = true;
            header.Shading.Color = Color.FromRgb(17, 46, 77);
            header.Format.Font.Bold = true;
            header.Format.Font.Color = Colors.White;
            for (int c = 0; c < tabela.Columns.Count; c++)
                header.Cells[c].AddParagraph(Cabecalho(tabela.Columns[c]));
            int indiceLinha = 0;
            foreach (DataRow registro in tabela.Rows)
            {
                var linha = grade.AddRow();
                if (indiceLinha++ % 2 == 1)
                    linha.Shading.Color = Color.FromRgb(245, 248, 252);
                for (int c = 0; c < tabela.Columns.Count; c++)
                {
                    string texto = Formatar(registro[c], tabela.Columns[c]);
                    if (tabela.Columns[c].ColumnName.StartsWith("quantidade") && registro[c] != DBNull.Value)
                        texto = Convert.ToDecimal(registro[c], Cultura).ToString("#,0.###", Cultura);
                    if (tabela.Columns[c].DataType == typeof(string))
                        texto = Regex.Replace(texto, @"(\S{12})(?=\S)", "$1\u200B");
                    linha.Cells[c].AddParagraph(texto);
                    if (Monetaria(tabela.Columns[c]) || tabela.Columns[c].ColumnName.StartsWith("quantidade"))
                        linha.Cells[c].Format.Alignment = ParagraphAlignment.Right;
                }
            }
            if (tabela.Rows.Count == 0)
                secao.AddParagraph("Nenhum registro encontrado.");
            var renderizador = new PdfDocumentRenderer { Document = documento };
            renderizador.RenderDocument();
            renderizador.PdfDocument.Save(caminho);
            renderizador.PdfDocument.Dispose();
        }

        private static void AdicionarGraficoPdf(Section secao, DataTable dados, ContextoRelatorio contexto, double largura)
        {
            var resumo = GraficoRelatorio.Resumir(dados, contexto.Titulo, contexto.TipoGrafico);
            var titulo = secao.AddParagraph(resumo.Titulo);
            titulo.Format.Font.Bold = true;
            titulo.Format.SpaceAfter = Unit.FromMillimeter(3);
            titulo.Format.KeepWithNext = true;
            if (resumo.Valores.Rows.Count == 0)
            {
                secao.AddParagraph("Sem valores para representar no gráfico.");
                return;
            }
            var composicao = secao.AddTable();
            composicao.AddColumn(Unit.FromCentimeter(largura * 0.38));
            composicao.AddColumn(Unit.FromCentimeter(largura * 0.62));
            var linha = composicao.AddRow();
            var valores = linha.Cells[0].Elements.AddTable();
            valores.AddColumn(Unit.FromCentimeter(largura * 0.21));
            valores.AddColumn(Unit.FromCentimeter(largura * 0.09));
            valores.AddColumn(Unit.FromCentimeter(largura * 0.07));
            var cabecalho = valores.AddRow();
            cabecalho.Shading.Color = Color.FromRgb(17, 46, 77);
            cabecalho.Format.Font.Bold = true;
            cabecalho.Format.Font.Color = Colors.White;
            cabecalho.Cells[0].AddParagraph("Grupo");
            cabecalho.Cells[1].AddParagraph(resumo.Monetario ? "Valor (R$)" : "Quantidade");
            cabecalho.Cells[2].AddParagraph("Percentual");
            valores.TopPadding = valores.BottomPadding = Unit.FromMillimeter(1.4);
            decimal soma = resumo.Valores.Rows.Cast<DataRow>().Sum(r => Convert.ToDecimal(r[1]));
            int indiceResumo = 0;
            foreach (DataRow item in resumo.Valores.Rows.Cast<DataRow>().Take(8))
            {
                var registro = valores.AddRow();
                if (indiceResumo++ % 2 == 1)
                    registro.Shading.Color = Color.FromRgb(245, 248, 252);
                registro.Cells[0].AddParagraph(Convert.ToString(item[0]));
                registro.Cells[1].AddParagraph(Convert.ToDecimal(item[1]).ToString(resumo.Monetario ? "C2" : "0.###", Cultura));
                registro.Cells[1].Format.Alignment = ParagraphAlignment.Right;
                registro.Cells[2].AddParagraph(soma == 0 ? "—" : (Convert.ToDecimal(item[1]) / soma).ToString("P1", Cultura));
                registro.Cells[2].Format.Alignment = ParagraphAlignment.Right;
            }
            if (resumo.Valores.Rows.Count > 8)
                linha.Cells[0].AddParagraph("Primeiros 8 grupos. O gráfico considera todos os grupos.");
            byte[] imagem = GraficoRelatorio.CriarImagem(dados, contexto.Titulo, contexto.TipoGrafico, true);
            linha.Cells[1].Format.Alignment = ParagraphAlignment.Center;
            var figura = linha.Cells[1].AddImage("base64:" + Convert.ToBase64String(imagem));
            figura.Width = Unit.FromCentimeter(largura * 0.62 - 0.4);
            figura.LockAspectRatio = true;
            var detalhe = secao.AddParagraph("Dados detalhados");
            detalhe.Format.Font.Bold = true;
            detalhe.Format.SpaceBefore = Unit.FromMillimeter(2);
            detalhe.Format.SpaceAfter = Unit.FromMillimeter(2);
            detalhe.Format.KeepWithNext = true;
        }

        private static double PesoColunaPdf(DataColumn coluna)
        {
            string nome = coluna.ColumnName;
            if (nome == "produto" || nome == "destino")
                return 2;
            if (nome == "origem")
                return 1.6;
            if (nome == "tipo")
                return 1;
            if (nome.StartsWith("quantidade"))
                return 0.8;
            if (coluna.DataType == typeof(DateTime))
                return 1.3;
            if (nome.Contains("observacao") || nome.Contains("descricao"))
                return 2.2;
            return 1.2;
        }
    }
}
