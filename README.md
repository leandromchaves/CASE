# CASE — Sistema de Gestão de Estoque e Eventos

O CASE é um sistema desktop desenvolvido como projeto acadêmico. Seu objetivo é ajudar uma organização a controlar mercadorias e realizar eventos, reunindo cadastros, equipes, escalas, movimentações de estoque e acompanhamento financeiro.

O sistema permite acompanhar os produtos desde o estoque central até os setores de um evento, registrar vendas e perdas e consultar os resultados conforme as permissões de cada usuário.

## Principais funcionalidades

- Solicitação de cadastro de usuários, aprovação ou rejeição administrativa e controle de acesso por perfil.
- Cadastros de produtos, categorias e fornecedores, com imagens de produtos e foto de perfil.
- Organização de eventos, setores, equipes, jornadas e escalas de trabalho.
- Estoque central da instituição, estoque geral de cada evento e estoques dos setores.
- Solicitações de mercadorias, decisões de aprovação ou rejeição e atendimento autorizado.
- Entradas, transferências, vendas, perdas, reposições e devoluções de sobras.
- Correções autorizadas de operações, com justificativa, histórico e atualização dos saldos.
- Despesas, conferências e reconferências financeiras, indicadores e relatórios.
- Exportação de relatórios em CSV, Excel (`.xlsx`) e PDF.
- Notificações internas e alteração da senha do usuário autenticado.

O cadastro público não permite escolher um perfil administrativo. Contas pendentes ou rejeitadas não podem entrar. A versão atual não utiliza envio de e-mails ou recuperação de senha por e-mail.

## Tecnologias utilizadas

| Tecnologia | Utilização |
|---|---|
| C# | Linguagem da aplicação e das regras de negócio. |
| Windows Forms | Interface gráfica desktop. |
| .NET Framework 4.7.2 | Plataforma de execução e compilação do projeto. |
| MySQL Server 8.0 | Armazenamento dos dados e integridade dos relacionamentos. |
| ADO.NET e MySql.Data | Conexões, comandos SQL parametrizados, leitura de resultados e transações. |
| Argon2id, por BouncyCastle.Cryptography | Geração e verificação dos hashes de senha. |
| ClosedXML | Exportação de planilhas Excel. |
| PDFsharp/MigraDoc | Geração direta de PDFs. |
| Visual Studio | Desenvolvimento, restauração de pacotes e compilação. |

O **MySQL Workbench** é recomendado para executar os scripts e consultar os dados. Ele é um cliente de administração: **não substitui o MySQL Server**, que precisa estar instalado e em execução.

## Bibliotecas e dependências

As versões abaixo estão declaradas em `CASE/packages.config` e referenciadas em `CASE/CASE.csproj`. Os usos diretos aparecem principalmente em `Conexao.cs`, nos Repositories, em `SenhaService.cs` e em `Utils/ExportadorRelatorio.cs`.

### Bibliotecas utilizadas diretamente e motor de PDF

| Biblioteca | Finalidade no CASE | Instalação |
|---|---|---|
| **MySql.Data 26.10.0** | Comunicação com o MySQL por ADO.NET: conexões, comandos parametrizados, Readers e transações. | Restaurada pelo NuGet. Exige MySQL Server em execução e configuração local de `CaseDb`; o pacote não instala o servidor. |
| **BouncyCastle.Cryptography 2.6.2** | Implementação do Argon2id utilizado por `SenhaService` para gerar e verificar hashes de senha. Também integra as dependências do driver MySQL. | Restaurada pelo NuGet; não exige serviço externo ou configuração manual dos parâmetros de hash. |
| **ClosedXML 0.105.1** | Criação e formatação dos relatórios Excel em `ExportadorRelatorio`, por meio de `XLWorkbook`. | Restaurada pelo NuGet com suas dependências. Não requer Microsoft Excel ou automação COM. |
| **PDFsharp-MigraDoc-GDI 6.2.4** | Composição de documentos e tabelas e renderização dos relatórios PDF com `PdfDocumentRenderer`. | Restaurada pelo NuGet. Utiliza a variante GDI para Windows; não exige impressora virtual. |
| **PDFsharp-GDI 6.2.4** | Motor de geração de PDF utilizado pelo MigraDoc. O projeto referencia suas DLLs; a composição dos relatórios é feita pelas APIs do MigraDoc. | Restaurada pelo NuGet como dependência do pacote MigraDoc. Não requer instalação de um aplicativo de PDF para gerar o arquivo. |

### Dependências de suporte

Os pacotes seguintes também fazem parte das referências atuais. Eles apoiam as bibliotecas acima; sua presença não significa que o CASE implemente APIs Google, injeção de dependência própria, assinatura digital ou outras funcionalidades independentes.

| Biblioteca | Finalidade no CASE | Instalação |
|---|---|---|
| Google.Protobuf 3.35.0 | Suporte de serialização utilizado internamente pelo MySql.Data. | Restauração pelo NuGet, sem configuração de serviços Google. |
| K4os.Compression.LZ4 1.3.8 e K4os.Compression.LZ4.Streams 1.3.8 | Suporte de compressão do driver MySQL. | Restauração pelo NuGet, sem instalação manual. |
| K4os.Hash.xxHash 1.0.8 | Funções auxiliares de hash das dependências de compressão; não é usado para proteger senhas dos usuários. | Restauração pelo NuGet, sem configuração adicional. |
| ZstdSharp.Port 0.8.8 | Suporte de compressão Zstandard do MySql.Data. | Restauração pelo NuGet, sem instalação manual. |
| System.Configuration.ConfigurationManager 10.0.8 | Suporte de configuração exigido pelo driver MySQL. A aplicação lê `CaseDb` pela API ConfigurationManager. | Restauração pelo NuGet; os valores de conexão continuam sendo configurados localmente. |
| ClosedXML.Parser 2.0.0 | Análise de expressões utilizada internamente pelo ClosedXML. | Restauração pelo NuGet, sem configuração adicional. |
| DocumentFormat.OpenXml 3.1.1 e DocumentFormat.OpenXml.Framework 3.1.1 | Estruturas e gravação do formato Open XML utilizadas na exportação `.xlsx`. | Restauração pelo NuGet; não requer Microsoft Office. |
| ExcelNumberFormat 1.1.0 | Interpretação dos formatos numéricos utilizados pelo ClosedXML. | Restauração pelo NuGet, sem configuração adicional. |
| SixLabors.Fonts 1.0.0 | Leitura e medição de fontes utilizadas pelo ClosedXML, inclusive no dimensionamento de colunas. | Restauração pelo NuGet; a aparência também depende das fontes disponíveis no Windows. |
| RBush.Signed 4.0.0 | Estrutura auxiliar utilizada internamente pelo ClosedXML. | Restauração pelo NuGet, sem configuração adicional. |
| Microsoft.Bcl.HashCode 1.1.1 | Compatibilidade de funções de hash exigidas pelo ClosedXML no .NET Framework. | Restauração pelo NuGet; não substitui o Argon2id. |
| Microsoft.Extensions.Logging.Abstractions 8.0.3 e Microsoft.Extensions.DependencyInjection.Abstractions 8.0.2 | Abstrações de suporte das dependências de PDF. Não representam um serviço de logs ou contêiner próprio configurado no CASE. | Restauração pelo NuGet, sem configuração adicional no aplicativo. |
| System.Security.Cryptography.Pkcs 8.0.2 | Suporte criptográfico das dependências PDFsharp/MigraDoc, atualizado para corrigir os avisos de segurança da versão 8.0.1. Sua referência não anuncia assinatura digital nos relatórios do CASE. | Restauração pelo NuGet, sem configuração de certificados para as exportações atuais. |
| Microsoft.Bcl.AsyncInterfaces 8.0.0 | Compatibilidade de interfaces exigidas pelas bibliotecas de suporte. | Restauração pelo NuGet, sem configuração adicional. |
| System.Buffers 4.6.0, System.Memory 4.6.0, System.Numerics.Vectors 4.6.0, System.Runtime.CompilerServices.Unsafe 6.1.0, System.Threading.Tasks.Extensions 4.5.4 e System.IO.Pipelines 5.0.2 | Recursos de memória, buffers, processamento e compatibilidade usados pelas dependências de banco e exportação. Apesar do prefixo `System`, estas versões são pacotes NuGet referenciados pelo projeto. | Restauração pelo NuGet; mantenha as DLLs e os redirecionamentos de versão gerados pela compilação junto à aplicação. |

**Recursos nativos:** Windows Forms (`System.Windows.Forms`), desenho e imagens (`System.Drawing`), as APIs base do ADO.NET (`System.Data`), arquivos (`System.IO`) e XML integram o .NET Framework. Não são bibliotecas externas a instalar separadamente pelo NuGet. O provedor MySQL é externo: o CASE usa MySql.Data para acessar esse banco pelas APIs ADO.NET. A exportação CSV usa recursos do próprio .NET, sem uma biblioteca CSV adicional.

### Como restaurar

No Visual Studio, utilize **Restaurar Pacotes NuGet** na solução. A restauração recupera as versões já declaradas na pasta `packages/`; não é necessário instalar cada item da tabela manualmente. Ela depende de acesso à origem dos pacotes ou de um cache local disponível e não instala o MySQL Server nem o Developer Pack do .NET Framework.

Como alternativa, no Developer PowerShell do Visual Studio:

```powershell
MSBuild.exe CASE.slnx /t:Restore /p:RestorePackagesConfig=true /verbosity:minimal
MSBuild.exe CASE.slnx /t:Rebuild /p:Configuration=Debug /verbosity:minimal
```

O parâmetro `RestorePackagesConfig=true` habilita a restauração do formato utilizado neste projeto. Não substitua essa operação por uma atualização automática de todos os pacotes. Para distribuir o aplicativo, mantenha as dependências copiadas para a saída de compilação junto de `CASE.exe`.

## Pré-requisitos

- Windows com suporte ao .NET Framework utilizado pelo projeto.
- Visual Studio com a carga de trabalho **Desenvolvimento para desktop com .NET**.
- **Developer Pack/Targeting Pack do .NET Framework 4.7.2** para compilar. Instalar somente o runtime não fornece todos os componentes de desenvolvimento.
- Uma versão do Visual Studio compatível com `CASE.slnx`. Se o formato não for reconhecido, abra diretamente `CASE/CASE.csproj`.
- MySQL Server 8.0 em execução e uma conta do MySQL com as permissões necessárias à instalação. A conta utilizada pela aplicação também precisa das permissões das operações autorizadas.
- MySQL Workbench, recomendado.
- Acesso ao NuGet para restaurar as dependências declaradas em `CASE/packages.config`.

Entre as dependências diretas atuais estão MySql.Data 26.10.0, BouncyCastle.Cryptography 2.6.2, ClosedXML 0.105.1 e PDFsharp-MigraDoc-GDI 6.2.4. Restaure também suas dependências; não copie somente uma DLL nem atualize versões indiscriminadamente. As exportações não exigem Microsoft Excel instalado ou impressora virtual de PDF.

## Como instalar e executar

### 1. Baixar o projeto

Use **Code → Download ZIP** no GitHub ou clone o repositório:

```powershell
git clone https://github.com/rafacayres/CASE.git
cd CASE
```

### 2. Abrir a solução

No Visual Studio, abra `CASE.slnx`, na raiz. O projeto da aplicação está em `CASE/CASE.csproj`.

### 3. Restaurar os pacotes NuGet

No Gerenciador de Soluções, clique com o botão direito na solução e selecione **Restaurar Pacotes NuGet**. Aguarde a conclusão antes de compilar. O projeto utiliza `packages.config`, com referências à pasta `packages/` da raiz.

### 4. Preparar o MySQL Server

Instale e inicie o MySQL Server 8.0. Anote o endereço, a porta e o usuário de acesso definidos na sua instalação. Esses dados são diferentes dos logins de Administrador, Gerente e Membro do CASE.

Para estudar e testar, utilize um ambiente local isolado, sem dados reais. Não reutilize uma base de uma organização.

### 5. Conectar pelo Workbench

Abra o MySQL Workbench e conecte-se ao servidor escolhido. Confira especialmente o host e a porta antes de executar SQL. Um schema com nome diferente no mesmo servidor e uma instância MySQL com outra porta são formas de isolamento distintas.

### 6. Localizar o script completo

O arquivo completo disponível nesta versão é **[`scripts/case_commercial.sql`](scripts/case_commercial.sql)**. Ele reúne estrutura, dados acadêmicos e consultas. Não é necessário executar os arquivos separados depois dele.

Os arquivos separados são:

| Arquivo | Conteúdo |
|---|---|
| `scripts/case_ddl.sql` | Estrutura do banco: tabelas, chaves, views, triggers e demais objetos. |
| `scripts/case_dml.sql` | Dados acadêmicos, incluindo as três contas de acesso. |
| `scripts/case_dql.sql` | 99 consultas documentadas, organizadas por funcionalidade. |

### 7. Executar em um banco novo e vazio

O script completo cria o schema `case_commercial`. **Não execute novamente sobre uma instalação que já contenha informações importantes.** Se o banco já existir ou ocorrer um erro na criação, interrompa a execução; não apague a base nem continue para misturar versões.

Para a demonstração acadêmica, prefira um schema isolado como `case_academico_estudo`. Abra **uma cópia** do script completo em uma aba do Workbench e substitua as referências ao schema `case_commercial` por `case_academico_estudo` nessa cópia. Não altere dados ou comandos por outros motivos. Execute somente quando esse schema ainda não existir no servidor de teste.

Abra o arquivo como UTF-8 e execute o conteúdo completo. Ele não depende de `SOURCE` ou de outro arquivo. A seção final contém muitas consultas: diversas abas de resultados no Workbench são esperadas.

### 8. Conferir a instalação e preparar o acesso

A estrutura atual possui **35 tabelas**. Na base acadêmica isolada, confira:

```sql
USE case_academico_estudo;
SELECT DATABASE() AS banco, @@port AS porta;
SHOW TABLES;
SELECT u.login, p.nome AS perfil, u.situacao_conta
FROM usuario u
JOIN perfilsistema p ON p.id_perfil_sistema = u.id_perfil_sistema
ORDER BY u.id_usuario;
```

`SHOW TABLES` também apresenta views; portanto, sua quantidade de linhas não corresponde apenas às tabelas.

Confira as contas da seção abaixo. Em uma instalação acadêmica nova com os scripts atualizados, os três logins utilizam a senha inicial `123`. Executar novamente o DML não redefine senhas de contas já existentes; não reinstale o banco para trocar credenciais.

### 9. Configurar a conexão CaseDb

O aplicativo lê a configuração `CaseDb` de `CASE/App.config`, por meio de `CASE/Data/Conexao.cs`. Ajuste-a somente na sua cópia local, usando os dados do MySQL da sua máquina. Exemplo de estrutura, com valores que precisam ser substituídos:

```xml
<connectionStrings>
  <add name="CaseDb"
       connectionString="Server=localhost;Port=3306;Database=case_academico_estudo;User ID=SEU_USUARIO_MYSQL;Password=SUA_SENHA_MYSQL;"
       providerName="MySql.Data.MySqlClient" />
</connectionStrings>
```

Confira também a porta: `3306` é apenas o valor do exemplo. Não publique sua senha MySQL, connection string privada ou cópias locais de configurações. Antes de versionar alterações, revise o diff para não incluir credenciais.

**Executar um SQL não altera automaticamente a conexão do CASE.** O nome configurado em `CaseDb` deve corresponder ao schema onde os dados foram instalados.

### 10. Compilar e iniciar

No Visual Studio, selecione **Debug**, compile a solução e execute com **F5** ou **Ctrl+F5**. A primeira janela é a de login.

Também é possível compilar pelo Developer PowerShell do Visual Studio, após restaurar os pacotes:

```powershell
MSBuild.exe CASE.slnx /t:Build /p:Configuration=Debug /verbosity:minimal
```

A saída fica em `CASE/bin/Debug/CASE.exe`. Para executar essa cópia, mantenha junto suas DLLs, recursos e `CASE.exe.config`. É esse arquivo de configuração, ao lado do executável, que contém a conexão usada em execução. Não copie apenas o `.exe`.

## Perfis de acesso

| Perfil | Responsabilidades |
|---|---|
| **Administrador** | Gerencia configurações institucionais e cadastros administrativos, aprova ou rejeita usuários, registra entradas no central, decide e atende solicitações e consulta informações administrativas. |
| **Gerente** | Organiza os próprios eventos, setores, equipes e escalas; cadastra e atualiza fornecedores; solicita mercadorias, realiza transferências autorizadas e acompanha operações e financeiro de seus eventos. Não recebe permissão para excluir fornecedores ou administrar o catálogo global de produtos. |
| **Membro** | Consulta suas escalas e estoques autorizados, registra vendas e perdas e solicita reposição. Suas consultas e relatórios são limitados aos dados permitidos. |

As restrições são aplicadas na interface e nas camadas de negócio e persistência. Não dependem apenas da visibilidade dos botões.

## Contas de demonstração

O DML inclui estas contas acadêmicas aprovadas:

| Perfil | Login | Senha |
|---|---|---|
| Administrador | `rafael.cayres` | `123` |
| Gerente | `leandro` | `123` |
| Membro | `pedro.almeida` | `123` |

Os hashes de `scripts/case_dml.sql` e do script completo `scripts/case_commercial.sql` foram gerados com `SenhaService.GerarHash("123")` e conferidos com `SenhaService.VerificarSenha`. Cada conta possui seu próprio salt. O banco armazena hashes **Argon2id**, não a senha em texto puro.

Essas credenciais são exclusivas para **testes acadêmicos locais e isolados**. Não utilize `123` em instalações reais, com dados de uma organização, em produção ou em sistemas públicos. Após entrar, a senha pode ser alterada em **Configurações → Perfil → Alterar senha**.

Para redefinir uma senha acadêmica esquecida em uma cópia isolada, utilize a ferramenta de console separada [`scripts/BootstrapPrimeiroAdministrador_CASE.cs`](scripts/BootstrapPrimeiroAdministrador_CASE.cs), conforme o procedimento abaixo. O modo `--redefinir-demo` utiliza SenhaService e aceita somente os três logins acadêmicos em schemas isolados com prefixo permitido. Ele recusa `case_commercial` nesse modo. Use a mesma base isolada na configuração da ferramenta e na execução de teste do CASE.

Não tente recuperar uma senha a partir do hash. Os materiais locais da pasta `documentacao/` não fazem parte da distribuição no GitHub; as instruções necessárias à instalação estão neste README.

## Como alterar a senha

Após entrar, acesse **Configurações → Perfil → Alterar senha**:

1. Digite a senha atual.
2. Escolha a nova senha.
3. Repita-a na confirmação e clique **Alterar senha**.

A operação rejeita senha atual incorreta, nova senha vazia, confirmação diferente e reutilização da senha atual. Após a gravação, a sessão é encerrada: entre novamente com a senha nova. O usuário altera somente a própria senha, identificada pela sessão autenticada.

## Primeiro administrador de uma instalação nova

Existem dois procedimentos diferentes:

- **Banco acadêmico:** o script completo ou o DML já cria as três contas. Não execute o bootstrap para criar outro administrador; prepare suas senhas no ambiente isolado.
- **Instalação sem usuários:** prepare somente a estrutura e os perfis necessários. Depois utilize o bootstrap controlado para escolher nome, login e senha do primeiro administrador, sem e-mail.

O bootstrap exige servidor local e schema `case_commercial`, habilitação explícita pelo operador e ausência de contas com perfil atribuído. Não é uma função de recuperação de senha nem deve substituir contas existentes.

Para testar essa inicialização sem tocar em um banco principal existente com esse mesmo nome, use uma **instância MySQL descartável separada**, com diretório de dados e porta próprios.

### Preparar a ferramenta de senhas

Depois de compilar o CASE, execute no Developer PowerShell, na raiz do repositório:

```powershell
$pastaFerramenta = Join-Path $env:LOCALAPPDATA 'CASE/FerramentasSenhas'
New-Item -ItemType Directory -Path $pastaFerramenta -Force | Out-Null
Get-ChildItem CASE/bin/Debug | Copy-Item -Destination $pastaFerramenta -Recurse -Force
$pastaCompilador = Split-Path (Get-Command MSBuild.exe).Source -Parent
if ((Split-Path $pastaCompilador -Leaf) -eq 'amd64') {
    $pastaCompilador = Split-Path $pastaCompilador -Parent
}
$csc = Join-Path $pastaCompilador 'Roslyn/csc.exe'
& $csc /nologo /target:exe "/out:$pastaFerramenta/BootstrapSenhas.exe" `
    "/reference:$pastaFerramenta/CASE.exe" `
    "/reference:$pastaFerramenta/MySql.Data.dll" `
    /reference:System.Configuration.dll /reference:System.Data.dll `
    scripts/BootstrapPrimeiroAdministrador_CASE.cs
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar a ferramenta.' }
Copy-Item "$pastaFerramenta/CASE.exe.config" "$pastaFerramenta/BootstrapSenhas.exe.config"
```

Revise `BootstrapSenhas.exe.config` nessa pasta separada: confirme host, porta e schema de `CaseDb`. Não publique essa configuração nem altere a conexão de uma instalação real para testar a ferramenta.

**Primeiro administrador:** em uma instalação nova, execute somente `case_ddl.sql` e prepare os três perfis em `perfilsistema`. Antes de inserir, consulte a tabela e cadastre somente os nomes que estiverem faltando. Exemplo para uma base nova conferida:

```sql
USE case_commercial;
SELECT @@hostname AS servidor, @@port AS porta, DATABASE() AS banco;
SELECT id_perfil_sistema, nome FROM perfilsistema;
INSERT INTO perfilsistema (nome)
SELECT 'Administrador'
WHERE NOT EXISTS (SELECT 1 FROM perfilsistema WHERE LOWER(TRIM(nome)) = 'administrador');
INSERT INTO perfilsistema (nome)
SELECT 'Gerente'
WHERE NOT EXISTS (SELECT 1 FROM perfilsistema WHERE LOWER(TRIM(nome)) = 'gerente');
INSERT INTO perfilsistema (nome)
SELECT 'Membro'
WHERE NOT EXISTS (SELECT 1 FROM perfilsistema WHERE LOWER(TRIM(nome)) = 'membro');
```

Não execute esse preparo sobre a base atual da organização. Com a configuração da ferramenta apontando à instalação nova, habilite o procedimento apenas durante a execução:

```powershell
$env:CASE_PERMITIR_BOOTSTRAP_ADMIN = 'CONFIRMAR_PRIMEIRO_ADMINISTRADOR'
try {
    & "$pastaFerramenta/BootstrapSenhas.exe" --criar-admin
} finally {
    Remove-Item Env:CASE_PERMITIR_BOOTSTRAP_ADMIN -ErrorAction SilentlyContinue
}
```

Confirme o nome do banco, informe nome e login, escolha e confirme a senha oculta e digite `CRIAR O PRIMEIRO ADMINISTRADOR`. A conta é gravada como Administrador aprovado, sem e-mail. Depois entre no CASE conectado à mesma instalação. A execução é bloqueada se já houver uma conta com perfil atribuído.

**Redefinição acadêmica:** configure a ferramenta para a base local isolada já populada, com prefixo `case_academico_`, `case_validacao_`, `case_homologacao_` ou `case_ui_`, e execute:

```powershell
& "$pastaFerramenta/BootstrapSenhas.exe" --redefinir-demo
```

Confirme o banco, informe um dos três logins acadêmicos e digite a nova senha duas vezes. A ferramenta altera somente o hash da conta selecionada e recusa `case_commercial` nesse modo. A senha não é exibida e não pode ser fornecida por redirecionamento da entrada. Esse utilitário não está integrado à recuperação de contas comuns.

## Funcionamento básico do estoque

```text
Estoque central da instituição
        ↓ solicitação, aprovação e atendimento
Estoque geral do evento
        ↓ distribuição autorizada
Estoques dos setores
        ↓
Vendas e perdas
```

O administrador registra entradas no central. O gerente solicita produtos para seu evento. **Aprovar uma solicitação não movimenta estoque:** o atendimento posterior verifica a disponibilidade e efetiva a transferência integral.

O gerente distribui os produtos aos setores. Os membros autorizados registram vendas e perdas, que reduzem o saldo do setor, ou solicitam reposição para análise e atendimento pelo gerente. Vendas registram o preço praticado e a forma de pagamento: dinheiro, Pix ou cartão.

As sobras dos setores retornam ao estoque geral. O gerente solicita a devolução ao central e o administrador confirma o recebimento físico pelo fluxo autorizado. O sistema valida saldo disponível e utiliza transações nas operações compostas. Correções autorizadas preservam auditoria; não se apaga o histórico para corrigir uma movimentação.

## Organização do projeto

```text
CASE.slnx
CASE/
├── Telas/          Interface: formulários e UserControls.
├── Models/         Objetos que representam os dados da aplicação.
├── Services/       Validações, permissões e regras de negócio.
├── Repositories/   Consultas e gravações no MySQL; inclui AcessoDados.
├── Data/           Criação da conexão a partir de CaseDb.
├── Utils/          Recursos compartilhados de interface e exportação.
├── Resources/      Imagens, ícones e demais recursos gráficos.
├── Quermesse/      Componentes complementares dos módulos de eventos.
├── App.config     Configuração local da aplicação.
└── packages.config
scripts/           Estrutura SQL, dados acadêmicos e consultas.
```

Em geral, o fluxo é **Tela → Service → Repository → MySQL**. Alguns Repositories utilizam `AcessoDados` para executar SQL e converter resultados; outros usam ADO.NET diretamente. As conexões, comandos e leitores são gerenciados com `using`.

## Informações acadêmicas

O CASE foi desenvolvido como projeto acadêmico para aplicar conhecimentos de programação orientada a objetos, interface desktop, banco de dados relacional e integração entre camadas. Os dados distribuídos para estudo representam um cenário acadêmico e não devem ser misturados a registros reais de uma organização.

Este README descreve a implementação e os arquivos atuais. Instalação do servidor, permissões locais, restauração dos pacotes e configuração das conexões precisam ser conferidas no computador em que o sistema será executado.
