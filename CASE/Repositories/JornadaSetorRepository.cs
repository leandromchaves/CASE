using System.Collections.Generic;
using System;
using CASE.Data;
using CASE.Models;
using MySql.Data.MySqlClient;

namespace CASE.Repositories
{
    public class JornadaSetorRepository
    {
        public List<JornadaSetor> ListarPorSetor(int idSetor)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_jornada,id_setor,`data`,id_turno FROM jornadasetor WHERE id_setor=@id ORDER BY `data`,id_jornada", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", idSetor);
                    using (MySqlDataReader leitorBanco = comandoBanco.ExecuteReader())
                    {
                        List<JornadaSetor> resultadoBanco = new List<JornadaSetor>();
                        while (leitorBanco.Read())
                        {
                            resultadoBanco.Add(ConverterLinha(leitorBanco));
                        }
                        return resultadoBanco;
                    }
                }
            }
        }

        public JornadaSetor BuscarPorId(int id)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_jornada,id_setor,`data`,id_turno FROM jornadasetor WHERE id_jornada=@id", conexaoBanco))
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

        public JornadaSetor BuscarPorSetorDataTurno(int idSetor, DateTime data, int idTurno)
        {
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("SELECT id_jornada,id_setor,`data`,id_turno FROM jornadasetor WHERE id_setor=@setor AND `data`=@data AND id_turno=@turno", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@setor", idSetor);
                    AcessoDados.AdicionarParametro(comandoBanco, "@data", data.Date);
                    AcessoDados.AdicionarParametro(comandoBanco, "@turno", idTurno);
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

        public int Cadastrar(JornadaSetor item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("INSERT INTO jornadasetor (id_setor,`data`,id_turno) VALUES (@setor,@data,@turno)", conexaoBanco))
                {
                    Vincular(comandoBanco, item);
                    comandoBanco.ExecuteNonQuery();
                    resultadoBanco = Convert.ToInt32(comandoBanco.LastInsertedId);
                }
            }
            return resultadoBanco;
        }

        public bool Atualizar(JornadaSetor item)
        {
            int resultadoBanco;
            using (MySqlConnection conexaoBanco = Conexao.CriarConexao())
            {
                conexaoBanco.Open();
                using (MySqlCommand comandoBanco = new MySqlCommand("UPDATE jornadasetor SET id_setor=@setor,`data`=@data,id_turno=@turno WHERE id_jornada=@id", conexaoBanco))
                {
                    AcessoDados.AdicionarParametro(comandoBanco, "@id", item.IdJornada);
                    Vincular(comandoBanco, item);
                    resultadoBanco = comandoBanco.ExecuteNonQuery();
                }
            }
            return resultadoBanco > 0;

        }

        private static void Vincular(MySqlCommand comando, JornadaSetor item)
        {
            AcessoDados.AdicionarParametro(comando, "@setor", item.IdSetor);
            AcessoDados.AdicionarParametro(comando, "@data", item.Data);
            AcessoDados.AdicionarParametro(comando, "@turno", item.IdTurno);
        }

        private static JornadaSetor ConverterLinha(MySqlDataReader leitor)
        {
            return new JornadaSetor
            {
                IdJornada = leitor.GetInt32("id_jornada"),
                IdSetor = leitor.GetInt32("id_setor"),
                Data = leitor.GetDateTime("data"),
                IdTurno = leitor.GetInt32("id_turno")
            };
        }
    }
}
