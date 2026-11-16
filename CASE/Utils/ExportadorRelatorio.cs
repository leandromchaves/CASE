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
            documento.Styles["Normal"].Font.Size = 8;
            var secao = documento.AddSection();
            bool muitoLargo = tabela.Columns.Count > 12;
            secao.PageSetup.PageFormat = muitoLargo ? PageFormat.A3 : PageFormat.A4;
            bool paisagem = tabela.Columns.Count > 6;
            secao.PageSetup.Orientation = paisagem ? Orientation.Landscape : Orientation.Portrait;
            secao.PageSetup.LeftMargin = secao.PageSetup.RightMargin = Unit.FromCentimeter(1.3);
            secao.PageSetup.TopMargin = Unit.FromCentimeter(1.8);
            secao.PageSetup.BottomMargin = Unit.FromCentimeter(1.6);
            var identidade = secao.Headers.Primary.AddParagraph("CASE" +
                (string.IsNullOrWhiteSpace(contexto.Instituicao) ? "" : " | " + contexto.Instituicao));
            identidade.Format.Font.Bold = true;
            identidade.Format.Font.Color = Color.FromRgb(23, 100, 192);
            var titulo = secao.AddParagraph(contexto.Titulo);
            titulo.Format.Font.Size = 17;
            titulo.Format.Font.Bold = true;
            titulo.Format.SpaceAfter = Unit.FromCentimeter(0.2);
            secao.AddParagraph("Emissão: " + contexto.Emissao.ToString("dd/MM/yyyy HH:mm", Cultura));
            var filtros = secao.AddParagraph(contexto.Filtros);
            filtros.Format.SpaceAfter = Unit.FromCentimeter(0.4);
            var rodape = secao.Footers.Primary.AddParagraph("CASE • Página ");
            rodape.AddPageField();
            rodape.AddText(" de ");
            rodape.AddNumPagesField();
            rodape.Format.Alignment = ParagraphAlignment.Right;
            rodape.Format.Font.Size = 8;
            var grade = secao.AddTable();
            grade.Borders.Color = Colors.LightGray;
            grade.Borders.Width = 0.25;
            grade.TopPadding = grade.BottomPadding = Unit.FromMillimeter(1.3);
            double largura = (muitoLargo ? 42 : paisagem ? 29.7 : 21) - 2.6;
            double[] pesos = tabela.Columns.Cast<DataColumn>().Select(c =>
                c.DataType == typeof(string)
                    ? (c.ColumnName.Contains("observacao") || c.ColumnName.Contains("descricao")
                        ? 2.2
                        : 1.5)
                    : 1.2).ToArray();
            for (int c = 0; c < tabela.Columns.Count; c++)
                grade.AddColumn(Unit.FromCentimeter(largura * pesos[c] / pesos.Sum()));
            var header = grade.AddRow();
            header.HeadingFormat = true;
            header.Shading.Color = Color.FromRgb(235, 241, 249);
            header.Format.Font.Bold = true;
            for (int c = 0; c < tabela.Columns.Count; c++)
                header.Cells[c].AddParagraph(Cabecalho(tabela.Columns[c]));
            foreach (DataRow registro in tabela.Rows)
            {
                var linha = grade.AddRow();
                for (int c = 0; c < tabela.Columns.Count; c++)
                {
                    string texto = Formatar(registro[c], tabela.Columns[c]);
                    if (tabela.Columns[c].DataType == typeof(string))
                        texto = Regex.Replace(texto, @"(\S{12})(?=\S)", "$1\u200B");
                    linha.Cells[c].AddParagraph(texto);
                    if (Monetaria(tabela.Columns[c]))
                        linha.Cells[c].Format.Alignment = ParagraphAlignment.Right;
                }
            }
            if (tabela.Rows.Count == 0)
                secao.AddParagraph("Nenhum registro encontrado.");
            secao.AddParagraph(tabela.Rows.Count + " registro(s).");
            foreach (DataColumn c in tabela.Columns)
                if (Totalizavel(tabela, c, contexto))
                    secao.AddParagraph("Total — " + Cabecalho(c) + ": " + Somar(tabela, c).ToString("C2", Cultura));
            var renderizador = new PdfDocumentRenderer { Document = documento };
            renderizador.RenderDocument();
            renderizador.PdfDocument.Save(caminho);
            renderizador.PdfDocument.Dispose();
        }
    }
}
