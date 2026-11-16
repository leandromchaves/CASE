using System;
using System.Collections.Generic;
using CASE.Models;
using CASE.Repositories;

namespace CASE.Services
{
    public class FornecedorService
    {
        private readonly FornecedorRepository fornecedorRepository = new FornecedorRepository();
        private readonly AutorizacaoService autorizacaoService = new AutorizacaoService();

        public List<Fornecedor> Listar()
        {
            throw new UnauthorizedAccessException("Informe a conta autenticada para consultar fornecedores.");
        }

        public List<Fornecedor> Listar(Usuario usuario)
        {
            autorizacaoService.ExigirAdministradorOuGerente(usuario);
            return fornecedorRepository.Listar();
        }

        public Fornecedor BuscarPorId(int idFornecedor)
        {
            throw new UnauthorizedAccessException("Informe a conta autenticada para consultar fornecedores.");
        }

        public Fornecedor BuscarPorId(Usuario usuario, int idFornecedor)
        {
            if (idFornecedor <= 0)
            {
                throw new ArgumentException(
                    "O identificador do fornecedor deve ser maior que zero.",
                    nameof(idFornecedor));
            }

            autorizacaoService.ExigirAdministradorOuGerente(usuario);
            return fornecedorRepository.BuscarPorId(idFornecedor);
        }

        public void Cadastrar(Fornecedor fornecedor)
        {
            throw new UnauthorizedAccessException("Informe a conta autenticada para cadastrar fornecedores.");
        }

        public void Cadastrar(Usuario usuario, Fornecedor fornecedor)
        {
            autorizacaoService.ExigirAdministradorOuGerente(usuario);
            if (fornecedor == null)
            {
                throw new ArgumentNullException(nameof(fornecedor));
            }

            ValidarCampos(fornecedor);

            fornecedor.Nome = fornecedor.Nome.Trim();
            fornecedorRepository.Cadastrar(fornecedor);
        }

        public void Atualizar(Fornecedor fornecedor)
        {
            throw new UnauthorizedAccessException("Informe a conta autenticada para atualizar fornecedores.");
        }

        public void Atualizar(Usuario usuario, Fornecedor fornecedor)
        {
            autorizacaoService.ExigirAdministradorOuGerente(usuario);
            if (fornecedor == null)
            {
                throw new ArgumentNullException(nameof(fornecedor));
            }

            if (fornecedor.IdFornecedor <= 0)
            {
                throw new ArgumentException(
                    "O identificador do fornecedor deve ser maior que zero.",
                    nameof(fornecedor));
            }

            ValidarCampos(fornecedor);

            fornecedor.Nome = fornecedor.Nome.Trim();
            fornecedorRepository.Atualizar(fornecedor);
        }

        public bool Excluir(Usuario usuario, int idFornecedor)
        {
            autorizacaoService.ExigirAdministrador(usuario);
            if (idFornecedor <= 0)
            {
                throw new ArgumentException("Selecione um fornecedor válido para excluir.");
            }

            try
            {
                // As FKs impedem remover fornecedores utilizados, inclusive sob concorrência.
                return fornecedorRepository.Excluir(idFornecedor);
            }
            catch (MySql.Data.MySqlClient.MySqlException erro)
            {
                if (erro.Number == 1451)
                {
                    throw new InvalidOperationException(
                        "O fornecedor possui vínculos com produtos, entradas ou despesas e não pode ser excluído.",
                        erro);
                }
                throw;
            }
        }

        private static void ValidarCampos(Fornecedor fornecedor)
        {
            if (string.IsNullOrWhiteSpace(fornecedor.Nome) || fornecedor.Nome.Trim().Length > 150)
            {
                throw new ArgumentException("Informe o nome do fornecedor com até 150 caracteres.");
            }
            if (fornecedor.Documento != null && fornecedor.Documento.Trim().Length > 18)
            {
                throw new ArgumentException("O documento pode ter até 18 caracteres.");
            }
            if (fornecedor.Telefone != null && fornecedor.Telefone.Trim().Length > 20)
            {
                throw new ArgumentException("O telefone pode ter até 20 caracteres.");
            }
            if (!string.IsNullOrWhiteSpace(fornecedor.Email))
            {
                string email = fornecedor.Email.Trim();
                if (email.Length > 150)
                {
                    throw new ArgumentException("O e-mail pode ter até 150 caracteres.");
                }
                try
                {
                    var endereco = new System.Net.Mail.MailAddress(email);
                    if (endereco.Address != email)
                    {
                        throw new ArgumentException("Informe um endereço de e-mail válido.");
                    }
                }
                catch (FormatException)
                {
                    throw new ArgumentException("Informe um endereço de e-mail válido.");
                }
            }
        }
    }
}
