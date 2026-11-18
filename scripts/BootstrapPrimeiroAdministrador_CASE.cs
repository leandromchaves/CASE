using System;
using System.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using CASE.Models;
using CASE.Services;
using MySql.Data.MySqlClient;

// Utilitário de console separado do CASE.exe. Não faz recuperação de senha de produção.
internal static class BootstrapPrimeiroAdministrador_CASE
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 1 || (args[0] != "--criar-admin" && args[0] != "--redefinir-demo"))
                throw new ArgumentException("Informe --criar-admin ou --redefinir-demo.");

            var configuracao = ConfigurationManager.ConnectionStrings["CaseDb"];
            if (configuracao == null) throw new InvalidOperationException("Configure CaseDb no arquivo do utilitário.");
            var destino = new MySqlConnectionStringBuilder(configuracao.ConnectionString);
            if (destino.Server != "localhost" && destino.Server != "127.0.0.1" && destino.Server != "::1")
                throw new InvalidOperationException("O procedimento exige servidor local.");
            bool demonstracao = args[0] == "--redefinir-demo";
            if (demonstracao && !Regex.IsMatch(destino.Database,
                @"\Acase_(validacao|ui|homologacao|academico)_[a-zA-Z0-9_]+\z"))
                throw new InvalidOperationException("Redefinição permitida somente em schema isolado identificado.");
            if (!demonstracao && destino.Database != "case_commercial")
                throw new InvalidOperationException("A inicialização exige case_commercial local, preparado e sem administrador.");

            using (var conexao = new MySqlConnection(destino.ConnectionString))
            {
                conexao.Open();
                if (conexao.Database != destino.Database) throw new InvalidOperationException("Banco inesperado.");
                Console.WriteLine("Servidor: " + destino.Server + "; banco: " + conexao.Database);
            }
            Console.Write("Digite o nome exato do banco para confirmar: ");
            if (!string.Equals(Console.ReadLine(), destino.Database, StringComparison.Ordinal))
                throw new InvalidOperationException("Confirmação do banco não confere.");

            string nome = null;
            if (!demonstracao) { Console.Write("Nome completo: "); nome = Console.ReadLine(); }
            Console.Write("Login: ");
            string login = Console.ReadLine();
            string senha = LerSenha("Senha: ");
            string confirmacao = LerSenha("Confirmar senha: ");
            if (string.IsNullOrWhiteSpace(senha) || !string.Equals(senha, confirmacao, StringComparison.Ordinal))
                throw new ArgumentException("Informe e confirme a mesma senha não vazia.");

            if (demonstracao)
                RedefinirDemonstracao(destino, login, senha);
            else
            {
                Console.Write("Digite CRIAR O PRIMEIRO ADMINISTRADOR: ");
                string autorizacao = Console.ReadLine();
                new PrimeiroAdministradorService().CriarPrimeiroAdministrador(
                    new Pessoa { Nome = nome }, login, senha, autorizacao);
            }
            Console.WriteLine("Operação concluída. Faça login com a senha escolhida.");
            return 0;
        }
        catch (Exception erro)
        {
            // Não imprime exceções de infraestrutura que possam conter informações sensíveis.
            Console.Error.WriteLine(erro is ArgumentException || erro is UnauthorizedAccessException
                || erro is InvalidOperationException ? erro.Message : "Não foi possível concluir a operação.");
            return 1;
        }
    }

    private static void RedefinirDemonstracao(MySqlConnectionStringBuilder destino, string login, string senha)
    {
        if (login != "rafael.cayres" && login != "leandro" && login != "pedro.almeida")
            throw new ArgumentException("Selecione uma das três contas do SQL acadêmico.");
        string hash = new SenhaService().GerarHash(senha);
        using (var conexao = new MySqlConnection(destino.ConnectionString))
        {
            conexao.Open();
            if (!string.Equals(conexao.Database, destino.Database, StringComparison.Ordinal) ||
                conexao.Database == "case_commercial")
                throw new InvalidOperationException("Banco não permitido para redefinição.");
            using (var transacao = conexao.BeginTransaction())
            {
                try
                {
                    using (var comando = new MySqlCommand(
                        "UPDATE usuario SET senha_hash=@hash WHERE login=@login", conexao, transacao))
                    {
                        comando.Parameters.AddWithValue("@hash", hash);
                        comando.Parameters.AddWithValue("@login", login);
                        if (comando.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("A conta acadêmica não foi encontrada.");
                    }
                    transacao.Commit();
                }
                catch
                {
                    transacao.Rollback();
                    throw;
                }
            }
        }
    }

    private static string LerSenha(string rotulo)
    {
        if (Console.IsInputRedirected)
            throw new InvalidOperationException("Digite a senha em um console interativo, sem redirecionamento.");
        Console.Write(rotulo);
        var texto = new StringBuilder();
        while (true)
        {
            ConsoleKeyInfo tecla = Console.ReadKey(true);
            if (tecla.Key == ConsoleKey.Enter) { Console.WriteLine(); return texto.ToString(); }
            if (tecla.Key == ConsoleKey.Escape) throw new InvalidOperationException("Operação cancelada.");
            if (tecla.Key == ConsoleKey.Backspace)
            {
                if (texto.Length > 0) texto.Length--;
            }
            else if (!char.IsControl(tecla.KeyChar)) texto.Append(tecla.KeyChar);
        }
    }
}
