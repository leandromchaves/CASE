using System;
using System.Collections.Generic;
using System.Globalization;

namespace CASE.Utils
{
    /// <summary>
    /// Converte valores técnicos do domínio em rótulos legíveis, sem alterar
    /// o conteúdo usado por regras de negócio ou persistência.
    /// </summary>
    public static class TextoInterface
    {
        private static Dictionary<string, string> Rotulos =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "pendente", "Pendente" },
                { "aprovado", "Aprovado" },
                { "aprovada", "Aprovada" },
                { "rejeitado", "Rejeitado" },
                { "rejeitada", "Rejeitada" },
                { "atendida", "Atendida" },
                { "confirmada", "Confirmada" },
                { "cancelado", "Cancelado" },
                { "cancelada", "Cancelada" },
                { "planejado", "Planejado" },
                { "em_andamento", "Em andamento" },
                { "finalizado", "Finalizado" },
                { "encerrado", "Encerrado" },
                { "suspenso", "Suspenso" },
                { "suspensa", "Suspensa" },
                { "ativo", "Ativo" },
                { "ativa", "Ativa" },
                { "inativo", "Inativo" },
                { "inativa", "Inativa" },
                { "liberacao", "Liberação" },
                { "devolucao", "Devolução" },
                { "compra", "Compra" },
                { "doacao", "Doação" },
                { "reposicao", "Reposição" },
                { "venda", "Venda" },
                { "perda", "Perda" },
                { "entrada", "Entrada" },
                { "transferencia", "Transferência" },
                { "saida", "Saída" },
                { "dinheiro", "Dinheiro" },
                { "pix", "Pix" },
                { "cartao", "Cartão" },
                { "em_estoque", "Em estoque" },
                { "sem_estoque", "Sem estoque" },
                { "estoque_baixo", "Estoque baixo" },
                { "abaixo_do_minimo", "Abaixo do mínimo" },
                { "normal", "Normal" },
                { "aguardando", "Aguardando" },
                { "concluido", "Concluído" },
                { "concluida", "Concluída" },
                { "nao_informado", "Não informado" },
                { "nao_disponivel", "Não disponível" }
            };

        public static string FormatarValorBanco(object valor)
        {
            if (valor == null || valor == DBNull.Value)
                return string.Empty;

            string texto = Convert.ToString(valor, CultureInfo.GetCultureInfo("pt-BR"));
            if (string.IsNullOrWhiteSpace(texto))
                return texto;

            texto = texto.Trim();
            string rotulo;
            if (Rotulos.TryGetValue(texto, out rotulo))
                return rotulo;

            if (texto.IndexOf('_') >= 0)
            {
                string legivel = texto.Replace('_', ' ').ToLower(CultureInfo.GetCultureInfo("pt-BR"));
                return CultureInfo.GetCultureInfo("pt-BR").TextInfo.ToTitleCase(legivel);
            }

            return texto.Length == 1
                ? texto.ToUpper(CultureInfo.GetCultureInfo("pt-BR"))
                : char.ToUpper(texto[0], CultureInfo.GetCultureInfo("pt-BR")) + texto.Substring(1);
        }

        public static string FormatarColunaBanco(string nomeColuna, object valor)
        {
            if (string.Equals(nomeColuna, "forma_pagamento", StringComparison.OrdinalIgnoreCase) &&
                (valor == null || valor == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(valor))))
            {
                return "Não informado";
            }

            if (string.Equals(nomeColuna, "situacao", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(nomeColuna, "status", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(nomeColuna, "tipo", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(nomeColuna, "forma_pagamento", StringComparison.OrdinalIgnoreCase))
            {
                return FormatarValorBanco(valor);
            }

            return valor == null || valor == DBNull.Value
                ? string.Empty
                : Convert.ToString(valor, CultureInfo.GetCultureInfo("pt-BR"));
        }
    }
}
