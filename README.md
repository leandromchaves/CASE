# CASE — Gestão de Estoque e Eventos

Sistema desktop desenvolvido como **projeto acadêmico**, para organizar eventos, controlar mercadorias e acompanhar vendas e resultados financeiros.

[Instalação](#como-executar) · [Contas de acesso](#contas-acadêmicas) · [Tecnologias](#tecnologias-e-bibliotecas) · [Banco de dados](#arquivos-do-banco)

## O que o sistema oferece

| Área | Funcionalidades |
|---|---|
| Pessoas | Cadastro, aprovação de usuários, perfis e alteração de senha. |
| Cadastros | Produtos, categorias, fornecedores e imagens. |
| Eventos | Setores, equipes, jornadas e escalas de trabalho. |
| Estoque | Entradas, solicitações, autorizações, transferências, reposições e devoluções. |
| Operações | Vendas, perdas e correções com histórico de alterações. |
| Acompanhamento | Dashboard, financeiro, notificações internas e relatórios com gráficos. |
| Exportações | CSV, Excel e PDF, com opção de incluir gráficos no Excel e no PDF. |

## Tecnologias e bibliotecas

**C# · Windows Forms · .NET Framework 4.7.2 · MySQL 8.0 · Visual Studio**

| Biblioteca | Para que serve |
|---|---|
| **MySQL Connector/NET** | Comunicação com o banco MySQL. |
| **Bouncy Castle** | Proteção das senhas com Argon2id. |
| **ClosedXML** | Geração de planilhas Excel. |
| **PDFsharp e MigraDoc** | Geração de documentos PDF. |

As bibliotecas são restauradas pelo **NuGet**. As versões ficam em [packages.config](CASE/packages.config). Não é necessário instalar Excel ou impressora virtual para exportar relatórios. Os gráficos usam os componentes do próprio .NET Framework.

## Como executar

### Antes de começar

- Windows e Visual Studio com **Desenvolvimento para desktop com .NET**.
- **Developer Pack do .NET Framework 4.7.2**, para compilar o projeto.
- **MySQL Server 8.0** instalado e em execução.
- **MySQL Workbench**, recomendado para executar o SQL; ele não substitui o servidor.

### Instalação

1. Baixe o projeto ou clone o repositório:

   ```powershell
   git clone https://github.com/rafacayres/CASE.git
   cd CASE
   ```

2. Abra `CASE.slnx` no Visual Studio. Se esse formato não for reconhecido, abra `CASE/CASE.csproj`.
3. Na solução, selecione **Restaurar Pacotes NuGet**.
4. Em uma instalação nova, execute [scripts/case_commercial.sql](scripts/case_commercial.sql) pelo Workbench. Ele cria a estrutura e os dados acadêmicos.
5. Em `CASE/App.config`, configure a conexão **CaseDb** com o seu servidor, usuário e senha MySQL:

   ```xml
   <add name="CaseDb"
        connectionString="Server=localhost;Port=3306;Database=case_commercial;User ID=SEU_USUARIO;Password=SUA_SENHA;"
        providerName="MySql.Data.MySqlClient" />
   ```

6. Compile em **Debug** e execute com **F5**.

> Execute o SQL completo apenas em uma base nova, sem dados importantes. O SQL não altera a conexão do aplicativo: o nome do banco em **CaseDb** deve corresponder ao banco criado. Não publique sua senha MySQL.

## Contas acadêmicas

| Perfil | Login | Senha | Acesso |
|---|---|---|---|
| **Administrador** | `rafael.cayres` | `123` | Informações gerais, cadastros administrativos, usuários e estoque central. |
| **Gerente** | `leandro` | `123` | Seus eventos, equipes, escalas, distribuição e financeiro; cadastro e edição de fornecedores compartilhados. |
| **Membro** | `pedro.almeida` | `123` | Suas escalas, vendas, perdas, reposições e consultas autorizadas. |

Essas credenciais são exclusivas para **demonstração acadêmica local e isolada**. Não use essas senhas com dados reais ou em sistemas públicos. As senhas são armazenadas como hashes Argon2id.

Para trocar a senha: **Configurações → Perfil → Alterar senha**. Informe a senha atual, a nova senha e a confirmação. Depois, entre novamente com a senha nova.

Novos usuários escolhem sua senha no cadastro e aguardam aprovação do Administrador.

## Fluxo básico

```text
Estoque central → Estoque geral do evento → Estoques dos setores → Vendas e perdas
```

O Administrador registra as entradas. O Gerente solicita mercadorias e distribui aos setores após aprovação e atendimento. Membros escalados registram vendas, perdas e pedidos de reposição.

**Aprovar não movimenta estoque:** a transferência ocorre no atendimento integral. As sobras retornam ao estoque geral e, após solicitação e confirmação administrativa, ao central.

## Arquivos do banco

| Arquivo | Conteúdo |
|---|---|
| [case_commercial.sql](scripts/case_commercial.sql) | Instalação completa: estrutura, dados e consultas. |
| [case_ddl.sql](scripts/case_ddl.sql) | Estrutura do banco. |
| [case_dml.sql](scripts/case_dml.sql) | Dados acadêmicos. |
| [case_dql.sql](scripts/case_dql.sql) | Consultas organizadas por funcionalidade. |

Após executar o arquivo completo, não é necessário executar os arquivos separados.

## Organização do código

**Telas → Services → Repositories → MySQL**

Em `CASE/`, a pasta **Telas** reúne a interface; **Models**, os objetos; **Services**, as regras e permissões; **Repositories** e **Data**, o acesso ao banco. **Utils** e **Resources** guardam os recursos compartilhados e as exportações.
