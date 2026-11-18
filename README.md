# CASE — Sistema de Gestão de Estoque e Eventos

Aplicação desktop desenvolvida como projeto acadêmico para controlar estoques, organizar eventos e acompanhar suas operações e resultados financeiros.

## Funcionalidades

- Cadastro e aprovação de usuários, com três perfis de acesso.
- Produtos, categorias, fornecedores e imagens.
- Eventos, setores, equipes, jornadas e escalas.
- Estoque central, estoque geral do evento e estoques dos setores.
- Entradas, solicitações, aprovações, transferências, reposições e devoluções.
- Vendas e perdas, com controle de saldo e correções auditadas.
- Despesas, conferências financeiras, indicadores e notificações internas.
- Relatórios com exportação em CSV, Excel e PDF.
- Perfil pessoal e alteração de senha.

## Tecnologias e bibliotecas

O CASE utiliza **C#, Windows Forms e .NET Framework 4.7.2**, com **MySQL 8.0** e acesso a dados por **ADO.NET**.

| Biblioteca | Finalidade |
|---|---|
| **MySql.Data** | Conexões, comandos SQL parametrizados, leitura de dados e transações no MySQL. |
| **BouncyCastle.Cryptography** | Proteção das senhas com Argon2id. |
| **ClosedXML** | Exportação de relatórios em Excel (`.xlsx`). |
| **PDFsharp/MigraDoc — GDI** | Geração direta de relatórios PDF. |

As bibliotecas e suas dependências são restauradas pelo **NuGet**. As versões estão em [`CASE/packages.config`](CASE/packages.config). Windows Forms faz parte do .NET Framework, e a exportação CSV utiliza recursos nativos. Não é necessário instalar Excel nem impressora virtual para gerar os relatórios.

## Pré-requisitos

- Windows.
- Visual Studio com **Desenvolvimento para desktop com .NET** e o **Developer Pack do .NET Framework 4.7.2**.
- MySQL Server 8.0 instalado e em execução.
- MySQL Workbench, recomendado para executar os scripts e consultar o banco.

O Workbench é um cliente de administração e **não substitui o MySQL Server**.

## Instalação

1. Baixe o projeto ou clone o repositório:

   ```powershell
   git clone https://github.com/rafacayres/CASE.git
   cd CASE
   ```

2. Abra `CASE.slnx` no Visual Studio. Se o formato não for reconhecido, abra `CASE/CASE.csproj`.
3. Clique com o botão direito na solução e selecione **Restaurar Pacotes NuGet**.
4. No Workbench, conecte-se ao servidor local e execute [`scripts/case_commercial.sql`](scripts/case_commercial.sql). Esse arquivo reúne estrutura, dados acadêmicos e consultas, criando o banco `case_commercial` com as três contas abaixo.
5. Configure a conexão **CaseDb** em `CASE/App.config` com os dados do seu MySQL:

   ```xml
   <add name="CaseDb"
        connectionString="Server=localhost;Port=3306;Database=case_commercial;User ID=SEU_USUARIO_MYSQL;Password=SUA_SENHA_MYSQL;"
        providerName="MySql.Data.MySqlClient" />
   ```

6. Compile em **Debug** e execute com **F5** ou **Ctrl+F5**.

**Use uma instalação local nova, sem dados importantes.** Não execute o script completo novamente sobre um banco em uso. Para testes separados, substitua o nome do schema em uma cópia do SQL e configure o mesmo nome em `CaseDb`.

Executar o SQL não altera automaticamente a conexão do aplicativo. Não publique sua senha MySQL. Ao executar fora do Visual Studio, mantenha as DLLs e o `CASE.exe.config` junto de `CASE.exe`, em `CASE/bin/Debug/`.

## Perfis e acessos acadêmicos

| Perfil | Responsabilidade | Login | Senha |
|---|---|---|---|
| **Administrador** | Cadastros institucionais, aprovação de usuários, estoque central e autorizações. | `rafael.cayres` | `123` |
| **Gerente** | Gestão dos próprios eventos, equipes, escalas, distribuição de mercadorias e financeiro. | `leandro` | `123` |
| **Membro** | Consulta das próprias escalas, registro de vendas e perdas e pedidos de reposição. | `pedro.almeida` | `123` |

Essas contas são exclusivas para **demonstração acadêmica local e isolada**. Não utilize essas senhas em produção ou com dados reais. O banco armazena hashes **Argon2id**, nunca a senha em texto puro.

Novos usuários escolhem sua senha no cadastro e aguardam aprovação administrativa. As permissões também são verificadas nos Services, além dos controles da interface.

### Alterar senha

Acesse **Configurações → Perfil → Alterar senha**, informe a senha atual, a nova senha e sua confirmação. Após salvar, a sessão é encerrada; entre novamente com a senha nova.

## Fluxo do estoque

```text
Estoque central → Estoque geral do evento → Estoques dos setores → Vendas e perdas
```

O Administrador registra as entradas no central. O Gerente solicita mercadorias e, após aprovação e atendimento administrativo, distribui os produtos aos setores. Os Membros escalados registram vendas e perdas ou solicitam reposição.

**Aprovar não movimenta o estoque:** a transferência ocorre no atendimento integral do pedido. As sobras retornam dos setores ao estoque geral e, mediante solicitação e confirmação administrativa, ao central. O sistema impede saldo negativo e preserva o histórico das correções.

## Organização do projeto

| Pasta | Conteúdo |
|---|---|
| `CASE/Telas` | Forms e UserControls da interface. |
| `CASE/Models` | Objetos e propriedades dos dados. |
| `CASE/Services` | Regras de negócio, validações e permissões. |
| `CASE/Repositories` | Consultas e gravações no MySQL. |
| `CASE/Data` | Criação das conexões com o banco. |
| `CASE/Utils` e `CASE/Resources` | Recursos compartilhados, exportações, imagens e ícones. |
| `scripts` | Arquivos SQL de instalação e estudo. |

O fluxo principal é **Tela → Service → Repository → MySQL**.

### Scripts SQL

| Arquivo | Conteúdo |
|---|---|
| [`case_commercial.sql`](scripts/case_commercial.sql) | Instalação completa: DDL, DML e DQL. |
| [`case_ddl.sql`](scripts/case_ddl.sql) | Estrutura do banco. |
| [`case_dml.sql`](scripts/case_dml.sql) | Dados acadêmicos. |
| [`case_dql.sql`](scripts/case_dql.sql) | 99 consultas documentadas por funcionalidade. |

Os arquivos separados servem para estudo e consulta. **Após executar o script completo, não é necessário executá-los novamente.**
