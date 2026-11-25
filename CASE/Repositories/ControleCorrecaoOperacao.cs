using System;
using System.Collections.Generic;
using CASE.Models;
using CASE.Services;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    // Revalida a identidade e os dados sob os mesmos bloqueios da correção.
    internal static class ControleCorrecaoOperacao
    {
        internal static string ValidarEditor(MySqlConnection conexao, MySqlTransaction transacao, int idUsuario)
        {
            Usuario sessao = SessaoUsuario.ObterUsuarioAtual();
            if (sessao.IdUsuario != idUsuario)
            {
                throw new UnauthorizedAccessException("A identidade da correção não corresponde à sessão.");
            }
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT ps.nome FROM usuario u JOIN perfilsistema ps ON ps.id_perfil_sistema=u.id_perfil_sistema " +
                "WHERE u.id_usuario=@id AND u.id_pessoa=@pessoa AND u.id_perfil_sistema=@perfil " +
                "AND u.situacao_conta='aprovado' FOR UPDATE", conexao, transacao))
            {
                comando.Parameters.AddWithValue("@id", idUsuario);
                comando.Parameters.AddWithValue("@pessoa", sessao.IdPessoa);
                comando.Parameters.AddWithValue("@perfil", sessao.IdPerfilSistema);
                string perfil = Convert.ToString(comando.ExecuteScalar());
                foreach (string permitido in new[] { "Administrador", "Gerente", "Membro" })
                {
                    if (string.Equals(perfil, permitido, StringComparison.OrdinalIgnoreCase))
                    {
                        return permitido;
                    }
                }
                throw new UnauthorizedAccessException("A conta não está autorizada a corrigir operações.");
            }
        }

        internal static void ValidarVersao(uint atual, uint esperada, string justificativa)
        {
            if (string.IsNullOrWhiteSpace(justificativa) || justificativa.Trim().Length > 500)
            {
                throw new ArgumentException("Informe a justificativa da correção, com até 500 caracteres.");
            }
            if (atual != esperada)
            {
                throw new InvalidOperationException("A operação foi alterada. Reabra os dados antes de corrigir novamente.");
            }
        }

        internal static void ValidarEvento(MySqlConnection conexao, MySqlTransaction transacao,
            int idEvento, string perfil, bool registro, int idAutor)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT situacao,id_responsavel FROM evento WHERE id_evento=@id FOR UPDATE", conexao, transacao))
            {
                comando.Parameters.AddWithValue("@id", idEvento);
                using (MySqlDataReader leitor = comando.ExecuteReader())
                {
                    if (!leitor.Read())
                    {
                        throw new InvalidOperationException("O evento da operação não foi encontrado.");
                    }
                    string situacao = leitor.GetString("situacao").Trim().ToLowerInvariant();
                    if (situacao != SituacoesEvento.EmAndamento && (registro || situacao != SituacoesEvento.Planejado))
                    {
                        throw new InvalidOperationException("A situação do evento não permite esta correção.");
                    }
                    Usuario sessao = SessaoUsuario.ObterUsuarioAtual();
                    if (perfil == "Administrador" ||
                        perfil == "Gerente" && leitor.GetInt32("id_responsavel") == sessao.IdPessoa ||
                        perfil == "Membro" && registro && idAutor == sessao.IdUsuario)
                    {
                        return;
                    }
                    throw new UnauthorizedAccessException("Você não pode corrigir operações deste evento.");
                }
            }
        }

        internal static Dictionary<int, int> LerItens(MySqlConnection conexao, MySqlTransaction transacao,
            string sql, int idOperacao, int quantidadeEsperada)
        {
            Dictionary<int, int> itens = new Dictionary<int, int>();
            using (MySqlCommand comando = new MySqlCommand(sql, conexao, transacao))
            {
                comando.Parameters.AddWithValue("@id", idOperacao);
                using (MySqlDataReader leitor = comando.ExecuteReader())
                {
                    while (leitor.Read())
                    {
                        itens.Add(leitor.GetInt32("id_item"), leitor.GetInt32("id_produto"));
                    }
                }
            }
            if (itens.Count == 0 || itens.Count != quantidadeEsperada)
            {
                throw new ArgumentException("A correção deve manter os itens originais da operação.");
            }
            return itens;
        }

        internal static void ValidarItem(Dictionary<int, int> originais, HashSet<int> informados,
            int idItem, int idProduto, decimal quantidade, decimal? valor, bool valorObrigatorio,
            bool permitirTrocaProduto = false)
        {
            int produtoOriginal;
            if (!originais.TryGetValue(idItem, out produtoOriginal) ||
                (!permitirTrocaProduto && produtoOriginal != idProduto) || !informados.Add(idItem))
            {
                throw new ArgumentException("Não é permitido trocar, remover ou duplicar os itens originais nesta correção.");
            }
            if (quantidade <= 0 || quantidade > 9999999 || quantidade != decimal.Truncate(quantidade))
            {
                throw new ArgumentException("Informe quantidades inteiras entre 1 e 9.999.999.");
            }
            if (valorObrigatorio && !valor.HasValue || valor.HasValue &&
                (valor.Value < 0 || valor.Value > 99999999.99M || decimal.Round(valor.Value, 2) != valor.Value))
            {
                throw new ArgumentException("Informe um valor válido com até duas casas decimais.");
            }
        }
    }
}
