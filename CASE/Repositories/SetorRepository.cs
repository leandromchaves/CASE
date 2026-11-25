using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class SetorRepository
    {
        public List<Setor> ListarPorEvento(int idEvento)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_setor,id_evento,nome FROM setor WHERE id_evento=@id ORDER BY nome,id_setor", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idEvento);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<Setor> resultadoBanco = new List<Setor>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public Setor BuscarPorId(int id)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_setor,id_evento,nome FROM setor WHERE id_setor=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", id);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        if (leitorBanco.Read())
                        {
                            return ConverterLinha(leitorBanco);
                        }
                        return null;
                    }
                }
            }
        }

        public int CadastrarComEstoqueDeBarraca(Setor item, int idUsuarioResponsavel)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();

                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        ValidarGerenteResponsavel(
                            conexao,
                            transacao,
                            item.IdEvento,
                            idUsuarioResponsavel);

                        int resultadoBanco2;
                        using (MySqlCommand comandoBanco2 = new MySqlCommand("INSERT INTO setor (id_evento,nome) VALUES (@evento,@nome)", conexao, transacao))
                        {
                            AcessoDados.AdicionarParametro(comandoBanco2, "@evento", item.IdEvento);
                            AcessoDados.AdicionarTextoObrigatorio(comandoBanco2, "@nome", item.Nome);
                            comandoBanco2.ExecuteNonQuery();
                            resultadoBanco2 = Convert.ToInt32(comandoBanco2.LastInsertedId);
                        }
                        int idSetor = resultadoBanco2;

                        int idInstituicao = BuscarInstituicaoDoEvento(
                            conexao,
                            transacao,
                            item.IdEvento);

                        int resultadoBanco3;
                        using (MySqlCommand comandoBanco3 = new MySqlCommand("INSERT INTO localestoque " +
                            "(id_instituicao_central,id_evento,nome,ativo,eh_geral_evento) " +
                            "VALUES (NULL,@evento,@nome,1,0)", conexao, transacao))
                        {
                            AcessoDados.AdicionarParametro(comandoBanco3, "@instituicao", idInstituicao);
                            AcessoDados.AdicionarParametro(comandoBanco3, "@evento", item.IdEvento);
                            AcessoDados.AdicionarTextoObrigatorio(comandoBanco3, "@nome", item.Nome);
                            comandoBanco3.ExecuteNonQuery();
                            resultadoBanco3 = Convert.ToInt32(comandoBanco3.LastInsertedId);
                        }
                        int idLocal = resultadoBanco3;

                        using (MySqlCommand comandoBanco4 = new MySqlCommand("INSERT INTO estoquesetor (id_setor,id_local_estoque,id_evento) " +
                            "VALUES (@setor,@local,@evento)", conexao, transacao))
                        {
                            AcessoDados.AdicionarParametro(comandoBanco4, "@setor", idSetor);
                            AcessoDados.AdicionarParametro(comandoBanco4, "@local", idLocal);
                            AcessoDados.AdicionarParametro(comandoBanco4, "@evento", item.IdEvento);
                            comandoBanco4.ExecuteNonQuery();
                        }

                        transacao.Commit();
                        return idSetor;
                    }
                    catch
                    {
                        transacao.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool Atualizar(Setor item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("UPDATE setor SET id_evento=@evento,nome=@nome WHERE id_setor=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", item.IdSetor);
                    AcessoDados.AdicionarParametro(comandoBanco, "@evento", item.IdEvento);
                    AcessoDados.AdicionarTextoObrigatorio(comandoBanco, "@nome", item.Nome);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco > 0;

        }

        public void RenomearComLocal(Setor setor, int idUsuario, string nomeAnterior)
        {
            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();
                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        ValidarGerenteResponsavel(conexao, transacao, setor.IdEvento, idUsuario);
                        int resultadoBanco6;
                        using (MySqlCommand comandoBanco6 = new MySqlCommand("UPDATE setor SET nome=@nome WHERE id_setor=@setor AND id_evento=@evento " +
                            "AND BINARY nome=BINARY @anterior", conexao, transacao))
                        {
                            AcessoDados.AdicionarParametro(comandoBanco6, "@setor", setor.IdSetor);
                            AcessoDados.AdicionarParametro(comandoBanco6, "@evento", setor.IdEvento);
                            AcessoDados.AdicionarParametro(comandoBanco6, "@nome", setor.Nome);
                            AcessoDados.AdicionarParametro(comandoBanco6, "@anterior", nomeAnterior);
                            resultadoBanco6 = comandoBanco6.ExecuteNonQuery();
                        }
                        int alterados = resultadoBanco6;
                        if (alterados != 1)
                        {
                            throw new InvalidOperationException("O setor foi alterado. Atualize a lista antes de editar.");
                        }
                        int resultadoBanco7;
                        using (MySqlCommand comandoBanco7 = new MySqlCommand("UPDATE localestoque l JOIN estoquesetor es ON es.id_local_estoque=l.id_local_estoque " +
                            "SET l.nome=@nome WHERE es.id_setor=@setor AND es.id_evento=@evento " +
                            "AND l.id_evento=@evento AND l.id_instituicao_central IS NULL AND l.eh_geral_evento=0", conexao, transacao))
                        {
                            AcessoDados.AdicionarParametro(comandoBanco7, "@setor", setor.IdSetor);
                            AcessoDados.AdicionarParametro(comandoBanco7, "@evento", setor.IdEvento);
                            AcessoDados.AdicionarParametro(comandoBanco7, "@nome", setor.Nome);
                            resultadoBanco7 = comandoBanco7.ExecuteNonQuery();
                        }
                        int locais = resultadoBanco7;
                        if (locais != 1)
                        {
                            throw new InvalidOperationException("O local de estoque do setor não está configurado corretamente.");
                        }
                        transacao.Commit();
                    }
                    catch (MySqlException erro)
                    {
                        transacao.Rollback();
                        if (erro.Number == 1062)
                        {
                            throw new InvalidOperationException("Já existe um setor ou local com esse nome no evento.", erro);
                        }
                        throw;
                    }
                    catch
                    {
                        transacao.Rollback();
                        throw;
                    }
                }
            }
        }

        public void ExcluirSeVazio(Setor setor, int idUsuario)
        {
            if (setor == null || setor.IdSetor <= 0 || setor.IdEvento <= 0 || idUsuario <= 0)
            {
                throw new ArgumentException("Os dados do setor são inválidos.", nameof(setor));
            }

            using (MySqlConnection conexao = Conexao.CriarConexao())
            {
                conexao.Open();
                using (MySqlTransaction transacao = conexao.BeginTransaction())
                {
                    try
                    {
                        ValidarGerenteResponsavel(
                            conexao,
                            transacao,
                            setor.IdEvento,
                            idUsuario);

                        using (MySqlCommand comando = new MySqlCommand(
                            "SELECT id_setor FROM setor WHERE id_setor=@setor AND id_evento=@evento FOR UPDATE",
                            conexao,
                            transacao))
                        {
                            AcessoDados.AdicionarParametro(comando, "@setor", setor.IdSetor);
                            AcessoDados.AdicionarParametro(comando, "@evento", setor.IdEvento);
                            if (comando.ExecuteScalar() == null)
                            {
                                throw new InvalidOperationException(
                                    "O setor não existe mais. Atualize a lista e tente novamente.");
                            }
                        }

                        long jornadas = ContarNaTransacao(
                            conexao,
                            transacao,
                            "SELECT COUNT(*) FROM jornadasetor WHERE id_setor=@id",
                            setor.IdSetor);
                        if (jornadas > 0)
                        {
                            throw new InvalidOperationException(
                                "Este setor já possui escalas ou jornadas. Remova primeiro os vínculos de trabalho para preservar o histórico.");
                        }

                        long registros = ContarNaTransacao(
                            conexao,
                            transacao,
                            "SELECT COUNT(*) FROM registroitem ri " +
                            "JOIN produtosetor ps ON ps.id_produto_setor=ri.id_produto_setor " +
                            "WHERE ps.id_setor=@id",
                            setor.IdSetor);
                        long solicitacoes = ContarNaTransacao(
                            conexao,
                            transacao,
                            "SELECT COUNT(*) FROM solicitacaoitem si " +
                            "JOIN produtosetor ps ON ps.id_produto_setor=si.id_produto_setor " +
                            "WHERE ps.id_setor=@id",
                            setor.IdSetor);
                        if (registros > 0 || solicitacoes > 0)
                        {
                            throw new InvalidOperationException(
                                "Este setor possui registros ou solicitações vinculadas e não pode ser removido.");
                        }

                        long associacoes = ContarNaTransacao(
                            conexao,
                            transacao,
                            "SELECT COUNT(*) FROM estoquesetor WHERE id_setor=@id AND id_evento=@evento",
                            setor.IdSetor,
                            setor.IdEvento);
                        if (associacoes != 1)
                        {
                            throw new InvalidOperationException(
                                "O estoque deste setor não está configurado corretamente. O setor não foi removido.");
                        }

                        int idLocal;
                        using (MySqlCommand comando = new MySqlCommand(
                            "SELECT id_local_estoque FROM estoquesetor WHERE id_setor=@setor AND id_evento=@evento FOR UPDATE",
                            conexao,
                            transacao))
                        {
                            AcessoDados.AdicionarParametro(comando, "@setor", setor.IdSetor);
                            AcessoDados.AdicionarParametro(comando, "@evento", setor.IdEvento);
                            idLocal = Convert.ToInt32(comando.ExecuteScalar());
                        }

                        long entradas = ContarNaTransacao(
                            conexao,
                            transacao,
                            "SELECT COUNT(*) FROM entradamercadoria WHERE id_local_destino=@id",
                            idLocal);
                        long transferencias = ContarNaTransacao(
                            conexao,
                            transacao,
                            "SELECT COUNT(*) FROM transferenciaestoque WHERE id_local_origem=@id OR id_local_destino=@id",
                            idLocal);
                        long saldo = ContarNaTransacao(
                            conexao,
                            transacao,
                            "SELECT COUNT(*) FROM estoque WHERE id_local_estoque=@id AND quantidade_atual<>0",
                            idLocal);

                        if (entradas > 0 || transferencias > 0 || saldo > 0)
                        {
                            throw new InvalidOperationException(
                                "Este setor já possui saldo ou movimentações. Para preservar o histórico do estoque, ele não pode ser removido.");
                        }

                        using (MySqlCommand comandoBanco8 = new MySqlCommand("DELETE FROM produtosetor WHERE id_setor=@setor", conexao, transacao))
                        {
                            AcessoDados.AdicionarParametro(comandoBanco8, "@setor", setor.IdSetor);
                            comandoBanco8.ExecuteNonQuery();
                        }

                        using (MySqlCommand comandoBanco9 = new MySqlCommand("DELETE FROM estoque WHERE id_local_estoque=@local", conexao, transacao))
                        {
                            AcessoDados.AdicionarParametro(comandoBanco9, "@local", idLocal);
                            comandoBanco9.ExecuteNonQuery();
                        }

                        int resultadoBanco10;
                        using (MySqlCommand comandoBanco10 = new MySqlCommand("DELETE FROM estoquesetor WHERE id_setor=@setor AND id_local_estoque=@local AND id_evento=@evento", conexao, transacao))
                        {
                            AcessoDados.AdicionarParametro(comandoBanco10, "@setor", setor.IdSetor);
                            AcessoDados.AdicionarParametro(comandoBanco10, "@local", idLocal);
                            AcessoDados.AdicionarParametro(comandoBanco10, "@evento", setor.IdEvento);
                            resultadoBanco10 = comandoBanco10.ExecuteNonQuery();
                        }
                        int associacaoRemovida = resultadoBanco10;
                        if (associacaoRemovida != 1)
                        {
                            throw new InvalidOperationException(
                                "O setor foi alterado durante a operação. Atualize a lista e tente novamente.");
                        }

                        int resultadoBanco11;
                        using (MySqlCommand comandoBanco11 = new MySqlCommand("DELETE FROM localestoque WHERE id_local_estoque=@local AND id_evento=@evento " +
                            "AND id_instituicao_central IS NULL AND eh_geral_evento=0", conexao, transacao))
                        {
                            AcessoDados.AdicionarParametro(comandoBanco11, "@local", idLocal);
                            AcessoDados.AdicionarParametro(comandoBanco11, "@evento", setor.IdEvento);
                            resultadoBanco11 = comandoBanco11.ExecuteNonQuery();
                        }
                        int localRemovido = resultadoBanco11;
                        if (localRemovido != 1)
                        {
                            throw new InvalidOperationException(
                                "O local de estoque do setor não pôde ser removido. Nenhuma alteração foi salva.");
                        }

                        int resultadoBanco12;
                        using (MySqlCommand comandoBanco12 = new MySqlCommand("DELETE FROM setor WHERE id_setor=@setor AND id_evento=@evento", conexao, transacao))
                        {
                            AcessoDados.AdicionarParametro(comandoBanco12, "@setor", setor.IdSetor);
                            AcessoDados.AdicionarParametro(comandoBanco12, "@evento", setor.IdEvento);
                            resultadoBanco12 = comandoBanco12.ExecuteNonQuery();
                        }
                        int setorRemovido = resultadoBanco12;
                        if (setorRemovido != 1)
                        {
                            throw new InvalidOperationException(
                                "O setor não foi removido. Atualize a lista e tente novamente.");
                        }

                        transacao.Commit();
                    }
                    catch (MySqlException erro)
                    {
                        transacao.Rollback();
                        if (erro.Number == 1451 || erro.Number == 1452)
                        {
                            throw new InvalidOperationException(
                                "Este setor possui informações vinculadas e não pode ser removido. O histórico foi preservado.",
                                erro);
                        }
                        throw;
                    }
                    catch
                    {
                        transacao.Rollback();
                        throw;
                    }
                }
            }
        }

        private static long ContarNaTransacao(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            string sql,
            int id,
            int? idEvento = null)
        {
            using (MySqlCommand comando = new MySqlCommand(sql, conexao, transacao))
            {
                AcessoDados.AdicionarParametro(comando, "@id", id);
                if (idEvento.HasValue)
                {
                    AcessoDados.AdicionarParametro(comando, "@evento", idEvento.Value);
                }
                return Convert.ToInt64(comando.ExecuteScalar());
            }
        }

        private static void ValidarGerenteResponsavel(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idEvento,
            int idUsuario)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT ev.id_evento FROM evento ev " +
                "JOIN usuario u ON u.id_usuario=@usuario " +
                "JOIN pessoa p ON p.id_pessoa=u.id_pessoa " +
                "JOIN perfilsistema ps ON ps.id_perfil_sistema=u.id_perfil_sistema " +
                "WHERE ev.id_evento=@evento AND (ps.nome='Administrador' OR " +
                "(ps.nome='Gerente' AND ev.id_responsavel=p.id_pessoa)) " +
                "AND ev.situacao IN ('planejado','em_andamento') " +
                "AND u.situacao_conta='aprovado' FOR UPDATE",
                conexao,
                transacao))
            {
                AcessoDados.AdicionarParametro(comando, "@evento", idEvento);
                AcessoDados.AdicionarParametro(comando, "@usuario", idUsuario);

                if (comando.ExecuteScalar() == null)
                {
                    throw new UnauthorizedAccessException(
                        "Somente o administrador ou o gerente responsável por um evento operacional pode alterar setores.");
                }
            }
        }

        private static int BuscarInstituicaoDoEvento(
            MySqlConnection conexao,
            MySqlTransaction transacao,
            int idEvento)
        {
            using (MySqlCommand comando = new MySqlCommand(
                "SELECT id_instituicao FROM evento WHERE id_evento=@evento FOR UPDATE",
                conexao,
                transacao))
            {
                AcessoDados.AdicionarParametro(comando, "@evento", idEvento);
                object resultado = comando.ExecuteScalar();

                if (resultado == null || resultado == DBNull.Value)
                {
                    throw new InvalidOperationException("O evento não foi encontrado.");
                }

                return Convert.ToInt32(resultado);
            }
        }

        private static Setor ConverterLinha(MySqlDataReader leitor)
        {
            return new Setor
            {
                IdSetor = leitor.GetInt32("id_setor"),
                IdEvento = leitor.GetInt32("id_evento"),
                Nome = leitor.GetString("nome")
            };
        }
    }
}
