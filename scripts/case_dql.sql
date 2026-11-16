-- Consultas das telas do CASE.
-- As variáveis abaixo representam os filtros usados nas telas.
-- Senhas e imagens não são exibidas nas consultas.
USE case_entrega_bd1;

SET NAMES utf8mb4 COLLATE utf8mb4_0900_ai_ci;

-- 01 — Contas e pessoas
-- 1. AnaliseUsuarioRepository.ListarPorUsuario
SET @id = (
    SELECT MIN(id_usuario)
    FROM usuario);

SELECT id_analise,
    id_usuario,
    id_analisador,
    decisao,
    data_hora,
    justificativa
FROM analiseusuario
WHERE id_usuario = @id
ORDER BY data_hora, id_analise;

-- 2. PerfilSistemaRepository.Listar
SELECT id_perfil_sistema,
    nome
FROM perfilsistema
ORDER BY id_perfil_sistema;

-- 3. PerfilSistemaRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_perfil_sistema)
    FROM perfilsistema);

SELECT id_perfil_sistema,
    nome
FROM perfilsistema
WHERE id_perfil_sistema = @id;

-- 4. PessoaRepository.Listar
SELECT id_pessoa,
    nome,
    telefone,
    email
FROM pessoa
ORDER BY id_pessoa;

-- 5. PessoaRepository.BuscarPorId / EquipeEventoRepository.BuscarPessoaPorId
SET @id = (
    SELECT MIN(id_pessoa)
    FROM pessoa);

SELECT id_pessoa,
    nome,
    telefone,
    email
FROM pessoa
WHERE id_pessoa = @id;

-- 6. PessoaRepository.BuscarFotoPerfil
SET @idPessoa = (
    SELECT id_pessoa
    FROM usuario
    WHERE login = 'pedro.almeida');

SELECT OCTET_LENGTH(foto_perfil) AS foto_bytes
FROM pessoa
WHERE id_pessoa = @idPessoa;

-- 7. UsuarioRepository.Listar
SELECT id_usuario,
    id_pessoa,
    id_perfil_sistema,
    login,
    situacao_conta,
    solicitado_em
FROM usuario
ORDER BY id_usuario;

-- 8. UsuarioRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_usuario)
    FROM usuario);

SELECT id_usuario,
    id_pessoa,
    id_perfil_sistema,
    login,
    situacao_conta,
    solicitado_em
FROM usuario
WHERE id_usuario = @id;

-- 9. UsuarioRepository.BuscarPorLogin
SET @login = 'pedro.almeida';

SELECT id_usuario,
    id_pessoa,
    id_perfil_sistema,
    login,
    situacao_conta,
    solicitado_em
FROM usuario
WHERE login = @login;

-- 10. UsuarioRepository.BuscarLoginPorPessoa
SET @pessoa = (
    SELECT id_pessoa
    FROM usuario
    WHERE login = 'leandro');

SELECT login
FROM usuario
WHERE id_pessoa = @pessoa;

-- 02 — Instituição e cadastros de apoio
-- 11. InstituicaoRepository.Listar
SELECT id_instituicao,
    nome,
    documento,
    telefone,
    email
FROM instituicao
ORDER BY id_instituicao;

-- 12. InstituicaoRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_instituicao)
    FROM instituicao);

SELECT id_instituicao,
    nome,
    documento,
    telefone,
    email
FROM instituicao
WHERE id_instituicao = @id;

-- 13. PerfilRepository.Listar
SELECT id_perfil,
    nome
FROM perfil
ORDER BY id_perfil;

-- 14. PerfilRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_perfil)
    FROM perfil);

SELECT id_perfil,
    nome
FROM perfil
WHERE id_perfil = @id;

-- 15. TipoEventoRepository.Listar
SELECT id_tipo_evento,
    nome
FROM tipoevento
ORDER BY id_tipo_evento;

-- 16. TipoEventoRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_tipo_evento)
    FROM tipoevento);

SELECT id_tipo_evento,
    nome
FROM tipoevento
WHERE id_tipo_evento = @id;

-- 17. TurnoRepository.Listar
SELECT id_turno,
    nome
FROM turno
ORDER BY id_turno;

-- 18. TurnoRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_turno)
    FROM turno);

SELECT id_turno,
    nome
FROM turno
WHERE id_turno = @id;

-- 03 — Produtos, categorias e fornecedores
-- 19. CategoriaRepository.Listar
SELECT id_categoria,
    nome
FROM categoria
ORDER BY nome, id_categoria;

-- 20. CategoriaRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_categoria)
    FROM categoria);

SELECT id_categoria,
    nome
FROM categoria
WHERE id_categoria = @id;

-- 21. FornecedorRepository.Listar
SELECT id_fornecedor,
    nome,
    documento,
    telefone,
    email
FROM fornecedor;

-- 22. FornecedorRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_fornecedor)
    FROM fornecedor);

SELECT id_fornecedor,
    nome,
    documento,
    telefone,
    email
FROM fornecedor
WHERE id_fornecedor = @id;

-- 23. ProdutoFornecedorRepository.ListarPorProduto
SET @id = (
    SELECT MIN(id_produto)
    FROM produtofornecedor);

SELECT id_produto,
    id_fornecedor,
    codigo_no_fornecedor
FROM produtofornecedor
WHERE id_produto = @id
ORDER BY id_fornecedor;

-- 24. ProdutoFornecedorRepository.BuscarPorChave
SET @fornecedor = (
    SELECT MIN(id_fornecedor)
    FROM fornecedor);

SET @produto = (
    SELECT MIN(id_produto)
    FROM produto);

SELECT id_produto,
    id_fornecedor,
    codigo_no_fornecedor
FROM produtofornecedor
WHERE id_produto = @produto
    AND id_fornecedor = @fornecedor;

-- 25. ProdutoRepository.Listar
SELECT id_produto,
    id_categoria,
    nome,
    descricao,
    unidade_medida,
    ativo,
    imagem
FROM produto
ORDER BY nome, id_produto;

-- 26. ProdutoRepository.Contar
SELECT COUNT(*)
FROM produto;

-- 27. ProdutoRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_produto)
    FROM produtofornecedor);

SELECT id_produto,
    id_categoria,
    nome,
    descricao,
    unidade_medida,
    ativo,
    imagem
FROM produto
WHERE id_produto = @id;

-- 04 — Eventos, equipes e escalas
-- 28. ConsultaOperacionalRepository.Escalas
SET @evento = (
    SELECT MIN(id_evento)
    FROM evento);

SET @perfil = 'Administrador';

SET @pessoa = (
    SELECT id_pessoa
    FROM usuario
    WHERE login = 'leandro');

SELECT e.id_escala,
    eq.id_pessoa,
    eq.ativo,
    e.situacao,
    j.id_jornada,
    j.data AS data_jornada,
    s.id_setor,
    CONCAT(ev.nome, ' / ', s.nome, ' / ', DATE_FORMAT(j.data, '%d/%m/%Y'), ' / ', p.nome, ' / ', e.entrada_prevista, ' - ', e.saida_prevista) AS descricao,
    CONCAT(s.nome, ' / ', DATE_FORMAT(j.data, '%d/%m/%Y'), ' / ', t.nome) AS descricao_jornada
FROM escala e
JOIN equipeevento eq ON eq.id_equipe = e.id_equipe
JOIN pessoa p ON p.id_pessoa = eq.id_pessoa
JOIN jornadasetor j ON j.id_jornada = e.id_jornada
JOIN turno t ON t.id_turno = j.id_turno
JOIN setor s ON s.id_setor = j.id_setor
JOIN evento ev ON ev.id_evento = s.id_evento
WHERE eq.id_evento = ev.id_evento
    AND (@perfil = 'Administrador'
        OR (@perfil = 'Gerente'
            AND ev.id_responsavel = @pessoa)
        OR (@perfil = 'Membro'
            AND eq.id_pessoa = @pessoa
            AND eq.ativo = 1))
    AND (@perfil <> 'Membro'
        OR LOWER(e.situacao) <> 'cancelada')
    AND (@evento = 0
        OR ev.id_evento = @evento)
ORDER BY j.data, s.nome, p.nome;

-- 29. EquipeEventoRepository.ListarPorEvento
SET @id = (
    SELECT MIN(id_evento)
    FROM evento);

SELECT id_equipe,
    id_evento,
    id_pessoa,
    id_perfil,
    ativo,
    id_usuario_criacao,
    criado_em,
    id_usuario_alteracao,
    atualizado_em,
    versao
FROM equipeevento
WHERE id_evento = @id
ORDER BY id_equipe;

-- 30. EquipeEventoRepository.ListarPorPessoa
SET @pessoa = (
    SELECT id_pessoa
    FROM usuario
    WHERE login = 'leandro');

SELECT id_equipe,
    id_evento,
    id_pessoa,
    id_perfil,
    ativo,
    id_usuario_criacao,
    criado_em,
    id_usuario_alteracao,
    atualizado_em,
    versao
FROM equipeevento
WHERE id_pessoa = @pessoa
    AND ativo = 1
ORDER BY id_evento, id_equipe;

-- 31. EquipeEventoRepository.ListarPessoasDisponiveis
SET @evento = (
    SELECT MIN(id_evento)
    FROM evento);

SELECT p.id_pessoa,
    p.nome,
    p.telefone,
    p.email
FROM pessoa p
INNER JOIN usuario u ON u.id_pessoa = p.id_pessoa
INNER JOIN perfilsistema ps ON ps.id_perfil_sistema = u.id_perfil_sistema
WHERE LOWER(u.situacao_conta) = 'aprovado'
    AND LOWER(ps.nome) = 'membro'
    AND NOT EXISTS (
    SELECT 1
    FROM equipeevento ee
    WHERE ee.id_evento = @evento
        AND ee.id_pessoa = p.id_pessoa
        AND ee.ativo = 1)
ORDER BY p.nome;

-- 32. EquipeEventoRepository.BuscarPessoaDisponivelPorLogin
SET @evento = (
    SELECT MIN(id_evento)
    FROM evento);

SET @login = 'pedro.almeida';

SELECT p.id_pessoa,
    p.nome,
    p.telefone,
    p.email
FROM pessoa p
INNER JOIN usuario u ON u.id_pessoa = p.id_pessoa
INNER JOIN perfilsistema ps ON ps.id_perfil_sistema = u.id_perfil_sistema
WHERE u.login = @login
    AND LOWER(u.situacao_conta) = 'aprovado'
    AND LOWER(ps.nome) = 'membro'
    AND NOT EXISTS (
    SELECT 1
    FROM equipeevento ee
    WHERE ee.id_evento = @evento
        AND ee.id_pessoa = p.id_pessoa
        AND ee.ativo = 1);

-- 33. EquipeEventoRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_equipe)
    FROM equipeevento);

SELECT id_equipe,
    id_evento,
    id_pessoa,
    id_perfil,
    ativo,
    id_usuario_criacao,
    criado_em,
    id_usuario_alteracao,
    atualizado_em,
    versao
FROM equipeevento
WHERE id_equipe = @id;

-- 34. EscalaRepository.ListarPorJornada
SET @id = (
    SELECT MIN(id_jornada)
    FROM jornadasetor);

SELECT id_escala,
    id_equipe,
    id_jornada,
    entrada_prevista,
    saida_prevista,
    situacao
FROM escala
WHERE id_jornada = @id
ORDER BY entrada_prevista, id_escala;

-- 35. EscalaRepository.ListarPorEquipe
SET @id = (
    SELECT MIN(id_equipe)
    FROM equipeevento);

SELECT id_escala,
    id_equipe,
    id_jornada,
    entrada_prevista,
    saida_prevista,
    situacao
FROM escala
WHERE id_equipe = @id
ORDER BY id_jornada, entrada_prevista;

-- 36. EscalaRepository.ListarPorEvento
SET @evento = (
    SELECT MIN(id_evento)
    FROM evento);

SELECT e.id_escala,
    e.id_equipe,
    e.id_jornada,
    e.entrada_prevista,
    e.saida_prevista,
    e.situacao
FROM escala e
INNER JOIN equipeevento ee ON ee.id_equipe = e.id_equipe
INNER JOIN jornadasetor js ON js.id_jornada = e.id_jornada
INNER JOIN setor s ON s.id_setor = js.id_setor
WHERE ee.id_evento = @evento
    AND s.id_evento = @evento
ORDER BY js.`data`, e.entrada_prevista, e.id_escala;

-- 37. EscalaRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_escala)
    FROM escala);

SELECT id_escala,
    id_equipe,
    id_jornada,
    entrada_prevista,
    saida_prevista,
    situacao
FROM escala
WHERE id_escala = @id;

-- 38. EventoRepository.ListarTodos
SELECT id_evento,
    id_instituicao,
    id_tipo_evento,
    nome,
    `local`,
    data_inicio,
    data_fim,
    id_responsavel,
    situacao,
    observacao,
    id_usuario_criacao,
    criado_em,
    versao
FROM evento
ORDER BY data_inicio, id_evento;

-- 39. EventoRepository.ListarPorInstituicao
SET @id = (
    SELECT MIN(id_instituicao)
    FROM instituicao);

SELECT id_evento,
    id_instituicao,
    id_tipo_evento,
    nome,
    `local`,
    data_inicio,
    data_fim,
    id_responsavel,
    situacao,
    observacao,
    id_usuario_criacao,
    criado_em,
    versao
FROM evento
WHERE id_instituicao = @id
ORDER BY data_inicio, id_evento;

-- 40. EventoRepository.ListarPorResponsavel
SET @responsavel = (
    SELECT id_pessoa
    FROM usuario
    WHERE login = 'leandro');

SELECT id_evento,
    id_instituicao,
    id_tipo_evento,
    nome,
    `local`,
    data_inicio,
    data_fim,
    id_responsavel,
    situacao,
    observacao,
    id_usuario_criacao,
    criado_em,
    versao
FROM evento
WHERE id_responsavel = @responsavel
ORDER BY data_inicio, id_evento;

-- 41. EventoRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_evento)
    FROM evento);

SELECT id_evento,
    id_instituicao,
    id_tipo_evento,
    nome,
    `local`,
    data_inicio,
    data_fim,
    id_responsavel,
    situacao,
    observacao,
    id_usuario_criacao,
    criado_em,
    versao
FROM evento
WHERE id_evento = @id;

-- 42. JornadaSetorRepository.ListarPorSetor
SET @id = (
    SELECT MIN(id_setor)
    FROM estoquesetor);

SELECT id_jornada,
    id_setor,
    `data`,
    id_turno
FROM jornadasetor
WHERE id_setor = @id
ORDER BY `data`, id_jornada;

-- 43. JornadaSetorRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_jornada)
    FROM jornadasetor);

SELECT id_jornada,
    id_setor,
    `data`,
    id_turno
FROM jornadasetor
WHERE id_jornada = @id;

-- 44. JornadaSetorRepository.BuscarPorSetorDataTurno
SET @data = CURDATE();

SET @setor = (
    SELECT MIN(id_setor)
    FROM setor);

SET @turno = (
    SELECT MIN(id_turno)
    FROM turno);

SELECT id_jornada,
    id_setor,
    `data`,
    id_turno
FROM jornadasetor
WHERE id_setor = @setor
    AND `data` = @data
    AND id_turno = @turno;

-- 45. SetorRepository.ListarPorEvento
SET @id = (
    SELECT MIN(id_evento)
    FROM evento);

SELECT id_setor,
    id_evento,
    nome
FROM setor
WHERE id_evento = @id
ORDER BY nome, id_setor;

-- 46. SetorRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_setor)
    FROM estoquesetor);

SELECT id_setor,
    id_evento,
    nome
FROM setor
WHERE id_setor = @id;

-- 05 — Locais e saldos de estoque
-- 47. ConsultaOperacionalRepository.Estoques
SET @perfil = 'Administrador';

SET @pessoa = (
    SELECT id_pessoa
    FROM usuario
    WHERE login = 'leandro');

SELECT l.id_local_estoque,
    l.nome AS local,
    p.id_produto,
    p.nome AS produto,
    p.unidade_medida,
    es.quantidade_atual,
    es.quantidade_minima,
    es.quantidade_ideal
FROM estoque es
JOIN localestoque l ON l.id_local_estoque = es.id_local_estoque
JOIN produto p ON p.id_produto = es.id_produto
LEFT JOIN evento ev ON ev.id_evento = l.id_evento
WHERE (@perfil = 'Administrador'
        OR (@perfil = 'Gerente'
            AND (ev.id_responsavel = @pessoa
                OR (l.eh_central = 1
                    AND EXISTS (
                    SELECT 1
                    FROM evento ce
                    WHERE ce.id_responsavel = @pessoa
                        AND ce.id_instituicao = l.id_instituicao))))
        OR (@perfil = 'Membro'
            AND EXISTS (
            SELECT 1
            FROM estoquesetor xs
            JOIN jornadasetor j ON j.id_setor = xs.id_setor
            JOIN escala e ON e.id_jornada = j.id_jornada
            JOIN equipeevento eq ON eq.id_equipe = e.id_equipe
            WHERE xs.id_local_estoque = l.id_local_estoque
                AND eq.id_evento = xs.id_evento
                AND eq.ativo = 1
                AND eq.id_pessoa = @pessoa
                AND LOWER(e.situacao) <> 'cancelada')))
ORDER BY l.nome, p.nome;

-- 48. EstoqueRepository.ListarPorLocal
SET @id = (
    SELECT MIN(id_local_estoque)
    FROM localestoque);

SELECT id_estoque,
    id_local_estoque,
    id_produto,
    quantidade_minima,
    quantidade_ideal,
    quantidade_atual
FROM estoque
WHERE id_local_estoque = @id
ORDER BY id_produto;

-- 49. EstoqueRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_estoque)
    FROM estoque);

SELECT id_estoque,
    id_local_estoque,
    id_produto,
    quantidade_minima,
    quantidade_ideal,
    quantidade_atual
FROM estoque
WHERE id_estoque = @id;

-- 50. EstoqueRepository.BuscarPorLocalEProduto
SET @local = (
    SELECT MIN(id_local_estoque)
    FROM localestoque);

SET @produto = (
    SELECT MIN(id_produto)
    FROM produto);

SELECT id_estoque,
    id_local_estoque,
    id_produto,
    quantidade_minima,
    quantidade_ideal,
    quantidade_atual
FROM estoque
WHERE id_local_estoque = @local
    AND id_produto = @produto;

-- 51. EstoqueSetorRepository.Listar
SELECT id_setor,
    id_local_estoque,
    id_evento
FROM estoquesetor
ORDER BY id_setor;

-- 52. EstoqueSetorRepository.BuscarPorSetor
SET @id = (
    SELECT MIN(id_setor)
    FROM estoquesetor);

SELECT id_setor,
    id_local_estoque,
    id_evento
FROM estoquesetor
WHERE id_setor = @id;

-- 53. LocalEstoqueRepository.Listar
SELECT id_local_estoque,
    id_instituicao,
    id_evento,
    nome,
    ativo,
    eh_central,
    eh_geral_evento
FROM localestoque
ORDER BY id_local_estoque;

-- 54. LocalEstoqueRepository.ListarCentrais
SET @instituicao = (
    SELECT MIN(id_instituicao)
    FROM instituicao);

SELECT id_local_estoque,
    id_instituicao,
    id_evento,
    nome,
    ativo,
    eh_central,
    eh_geral_evento
FROM localestoque
WHERE id_instituicao = @instituicao
    AND eh_central = 1
ORDER BY id_local_estoque;

-- 55. LocalEstoqueRepository.ListarPorEvento
SET @evento = (
    SELECT MIN(id_evento)
    FROM evento);

SELECT id_local_estoque,
    id_instituicao,
    id_evento,
    nome,
    ativo,
    eh_central,
    eh_geral_evento
FROM localestoque
WHERE id_evento = @evento
ORDER BY id_local_estoque;

-- 56. LocalEstoqueRepository.ListarGeraisPorEvento
SET @evento = (
    SELECT MIN(id_evento)
    FROM evento);

SELECT id_local_estoque,
    id_instituicao,
    id_evento,
    nome,
    ativo,
    eh_central,
    eh_geral_evento
FROM localestoque
WHERE id_evento = @evento
    AND eh_geral_evento = 1
ORDER BY id_local_estoque;

-- 57. LocalEstoqueRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_local_estoque)
    FROM localestoque);

SELECT id_local_estoque,
    id_instituicao,
    id_evento,
    nome,
    ativo,
    eh_central,
    eh_geral_evento
FROM localestoque
WHERE id_local_estoque = @id;

-- 06 — Entradas de mercadorias
-- 58. EntradaItemRepository.ListarPorEntrada
SET @id = (
    SELECT MIN(id_entrada)
    FROM entradamercadoria);

SELECT id_entrada_item,
    id_entrada,
    id_produto,
    quantidade,
    custo_unitario
FROM entradaitem
WHERE id_entrada = @id
ORDER BY id_entrada_item;

-- 59. EntradaMercadoriaRepository.ListarPorLocalDestino
SET @id = (
    SELECT MIN(id_entrada)
    FROM entradamercadoria);

SELECT id_entrada,
    id_fornecedor,
    id_local_destino,
    id_usuario_responsavel,
    tipo_entrada,
    data_hora,
    numero_documento,
    observacao,
    chave_operacao,
    criado_em,
    id_usuario_alteracao,
    atualizado_em,
    versao
FROM entradamercadoria
WHERE id_local_destino = @id
ORDER BY data_hora DESC, id_entrada DESC;

-- 60. EntradaMercadoriaRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_entrada)
    FROM entradamercadoria);

SELECT id_entrada,
    id_fornecedor,
    id_local_destino,
    id_usuario_responsavel,
    tipo_entrada,
    data_hora,
    numero_documento,
    observacao,
    chave_operacao,
    criado_em,
    id_usuario_alteracao,
    atualizado_em,
    versao
FROM entradamercadoria
WHERE id_entrada = @id;

-- 07 — Solicitações e atendimento
-- 61. SolicitacaoEstoqueCentralRepository.ListarPendentes
SET @situacao = 'pendente';

SELECT id_solicitacao_estoque_central,
    tipo,
    id_instituicao,
    id_evento,
    id_local_origem,
    id_local_destino,
    id_usuario_solicitante,
    data_hora,
    situacao,
    id_usuario_decisor,
    decidida_em,
    justificativa_rejeicao,
    versao
FROM solicitacaoestoquecentral
WHERE situacao = @situacao
ORDER BY data_hora, id_solicitacao_estoque_central;

-- 62. SolicitacaoEstoqueCentralRepository.ListarAprovadasSemAtendimento
SET @situacao = 'aprovada';

SELECT id_solicitacao_estoque_central,
    tipo,
    id_instituicao,
    id_evento,
    id_local_origem,
    id_local_destino,
    id_usuario_solicitante,
    data_hora,
    situacao,
    id_usuario_decisor,
    decidida_em,
    justificativa_rejeicao,
    versao
FROM solicitacaoestoquecentral s
WHERE s.situacao = @situacao
    AND NOT EXISTS (
    SELECT 1
    FROM transferenciaestoque t
    WHERE t.id_solicitacao_estoque_central = s.id_solicitacao_estoque_central)
ORDER BY s.data_hora, s.id_solicitacao_estoque_central;

-- 63. SolicitacaoEstoqueCentralRepository.ListarPorSolicitante
SET @usuario = (
    SELECT id_usuario
    FROM usuario
    WHERE login = 'leandro');

SELECT id_solicitacao_estoque_central,
    tipo,
    id_instituicao,
    id_evento,
    id_local_origem,
    id_local_destino,
    id_usuario_solicitante,
    data_hora,
    situacao,
    id_usuario_decisor,
    decidida_em,
    justificativa_rejeicao,
    versao
FROM solicitacaoestoquecentral
WHERE id_usuario_solicitante = @usuario
ORDER BY data_hora DESC, id_solicitacao_estoque_central DESC;

-- 64. SolicitacaoEstoqueCentralRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_solicitacao_estoque_central)
    FROM solicitacaoestoquecentral);

SELECT id_solicitacao_estoque_central,
    tipo,
    id_instituicao,
    id_evento,
    id_local_origem,
    id_local_destino,
    id_usuario_solicitante,
    data_hora,
    situacao,
    id_usuario_decisor,
    decidida_em,
    justificativa_rejeicao,
    versao
FROM solicitacaoestoquecentral
WHERE id_solicitacao_estoque_central = @id;

-- 65. SolicitacaoEstoqueCentralRepository.ListarItens
SET @id = (
    SELECT MIN(id_solicitacao_estoque_central)
    FROM solicitacaoestoquecentral);

SELECT id_solicitacao_estoque_central_item,
    id_solicitacao_estoque_central,
    id_produto,
    quantidade
FROM solicitacaoestoquecentralitem
WHERE id_solicitacao_estoque_central = @id
ORDER BY id_produto;

-- 66. SolicitacaoEstoqueCentralRepository.BuscarIdTransferenciaAtendimento
SET @id = (
    SELECT MIN(id_solicitacao_estoque_central)
    FROM solicitacaoestoquecentral);

SELECT id_transferencia
FROM transferenciaestoque
WHERE id_solicitacao_estoque_central = @id;

-- 67. SolicitacaoItemRepository.ListarPorSolicitacao
SET @id = (
    SELECT MIN(id_solicitacao)
    FROM solicitacao);

SELECT id_solicitacao_item,
    id_solicitacao,
    id_produto_setor,
    quantidade
FROM solicitacaoitem
WHERE id_solicitacao = @id
ORDER BY id_solicitacao_item;

-- 68. SolicitacaoRepository.ListarPorEscala
SET @id = (
    SELECT MIN(id_escala)
    FROM escala);

SELECT id_solicitacao,
    id_escala,
    tipo,
    descricao,
    data_hora,
    situacao,
    id_usuario_solicitante,
    id_usuario_decisor,
    decidida_em,
    justificativa_rejeicao,
    criado_em,
    versao
FROM solicitacao
WHERE id_escala = @id
ORDER BY data_hora DESC, id_solicitacao DESC;

-- 69. SolicitacaoRepository.ListarPorSolicitante
SET @usuario = (
    SELECT id_usuario
    FROM usuario
    WHERE login = 'pedro.almeida');

SELECT id_solicitacao,
    id_escala,
    tipo,
    descricao,
    data_hora,
    situacao,
    id_usuario_solicitante,
    id_usuario_decisor,
    decidida_em,
    justificativa_rejeicao,
    criado_em,
    versao
FROM solicitacao
WHERE id_usuario_solicitante = @usuario
ORDER BY data_hora DESC, id_solicitacao DESC;

-- 70. SolicitacaoRepository.ListarPorEvento
SET @evento = (
    SELECT MIN(id_evento)
    FROM evento);

SELECT DISTINCT so.id_solicitacao,
    so.id_escala,
    so.tipo,
    so.descricao,
    so.data_hora,
    so.situacao,
    so.id_usuario_solicitante,
    so.id_usuario_decisor,
    so.decidida_em,
    so.justificativa_rejeicao,
    so.criado_em,
    so.versao
FROM solicitacao so
JOIN escala e ON e.id_escala = so.id_escala
JOIN equipeevento ee ON ee.id_equipe = e.id_equipe
JOIN jornadasetor j ON j.id_jornada = e.id_jornada
JOIN setor s ON s.id_setor = j.id_setor
WHERE ee.id_evento = @evento
    AND s.id_evento = @evento
ORDER BY so.data_hora DESC, so.id_solicitacao DESC;

-- 71. SolicitacaoRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_solicitacao)
    FROM solicitacao);

SELECT id_solicitacao,
    id_escala,
    tipo,
    descricao,
    data_hora,
    situacao,
    id_usuario_solicitante,
    id_usuario_decisor,
    decidida_em,
    justificativa_rejeicao,
    criado_em,
    versao
FROM solicitacao
WHERE id_solicitacao = @id;

-- 72. SolicitacaoRepository.ListarPendentesPorEvento
SET @evento = (
    SELECT MIN(id_evento)
    FROM evento);

SET @situacao = 'pendente';

SELECT DISTINCT so.id_solicitacao,
    so.id_escala,
    so.tipo,
    so.descricao,
    so.data_hora,
    so.situacao,
    so.id_usuario_solicitante,
    so.id_usuario_decisor,
    so.decidida_em,
    so.justificativa_rejeicao,
    so.criado_em,
    so.versao
FROM solicitacao so
JOIN escala e ON e.id_escala = so.id_escala
JOIN equipeevento ee ON ee.id_equipe = e.id_equipe
JOIN jornadasetor j ON j.id_jornada = e.id_jornada
JOIN setor s ON s.id_setor = j.id_setor
WHERE ee.id_evento = @evento
    AND s.id_evento = @evento
    AND so.situacao = @situacao
ORDER BY so.data_hora, so.id_solicitacao;

-- 08 — Movimentações, correções e auditoria
-- 73. ConsultaOperacionalRepository.Movimentacoes
SET @fim = DATE_ADD(CURDATE(), INTERVAL 2 DAY);

SET @inicio = DATE_SUB(CURDATE(), INTERVAL 31 DAY);

SET @perfil = 'Administrador';

SET @pessoa = (
    SELECT id_pessoa
    FROM usuario
    WHERE login = 'leandro');

SELECT m.data_hora,
    m.tipo,
    m.produto,
    m.quantidade,
    m.origem,
    m.destino,
    m.responsavel,
    m.id_origem,
    m.id_destino,
    m.id_operacao,
    m.entidade
FROM(
    SELECT en.data_hora,
        'entrada' AS tipo,
        p.nome AS produto,
        i.quantidade,
        '' AS origem,
        l.nome AS destino,
        u.login AS responsavel,
        NULL AS evento,
        0 AS id_origem,
        l.id_local_estoque AS id_destino,
        en.id_entrada AS id_operacao,
        'entradamercadoria' AS entidade
    FROM entradamercadoria en
    JOIN entradaitem i ON i.id_entrada = en.id_entrada
    JOIN produto p ON p.id_produto = i.id_produto
    JOIN localestoque l ON l.id_local_estoque = en.id_local_destino
    JOIN usuario u ON u.id_usuario = en.id_usuario_responsavel
    WHERE @perfil = 'Administrador'
    UNION ALL
    SELECT tr.data_hora,
        'transferencia',
        p.nome,
        i.quantidade,
        o.nome,
        d.nome,
        u.login,
        COALESCE(o.id_evento, d.id_evento),
        o.id_local_estoque,
        d.id_local_estoque,
        tr.id_transferencia,
        'transferenciaestoque'
    FROM transferenciaestoque tr
    JOIN transferenciaitem i ON i.id_transferencia = tr.id_transferencia
    JOIN produto p ON p.id_produto = i.id_produto
    JOIN localestoque o ON o.id_local_estoque = tr.id_local_origem
    JOIN localestoque d ON d.id_local_estoque = tr.id_local_destino
    JOIN usuario u ON u.id_usuario = tr.id_usuario_responsavel
    WHERE @perfil <> 'Membro'
    UNION ALL
    SELECT r.data_hora,
        r.tipo,
        p.nome,
        i.quantidade,
        s.nome,
        '',
        u.login,
        ev.id_evento,
        es.id_local_estoque,
        0,
        r.id_registro,
        'registro'
    FROM registro r
    JOIN registroitem i ON i.id_registro = r.id_registro
    JOIN produtosetor ps ON ps.id_produto_setor = i.id_produto_setor
    JOIN produto p ON p.id_produto = ps.id_produto
    JOIN escala e ON e.id_escala = r.id_escala
    JOIN equipeevento eq ON eq.id_equipe = e.id_equipe
    JOIN jornadasetor j ON j.id_jornada = e.id_jornada
    JOIN setor s ON s.id_setor = j.id_setor
    JOIN evento ev ON ev.id_evento = s.id_evento
    JOIN estoquesetor es ON es.id_setor = s.id_setor
    JOIN usuario u ON u.id_usuario = r.id_usuario_responsavel
    WHERE eq.id_evento = ev.id_evento
        AND ps.id_setor = s.id_setor
        AND (@perfil = 'Administrador'
            OR (@perfil = 'Gerente'
                AND ev.id_responsavel = @pessoa)
            OR (@perfil = 'Membro'
                AND eq.id_pessoa = @pessoa
                AND eq.ativo = 1))
        AND (@perfil <> 'Membro'
            OR u.id_pessoa = @pessoa)) m
LEFT JOIN evento ev ON ev.id_evento = m.evento
WHERE m.data_hora >= @inicio
    AND m.data_hora < @fim
    AND (@perfil = 'Administrador'
        OR (@perfil = 'Gerente'
            AND ev.id_responsavel = @pessoa)
        OR (@perfil = 'Membro'
            AND m.tipo IN ('venda', 'perda')))
ORDER BY m.data_hora DESC;

-- 74. ConsultaOperacionalRepository.ItensParaCorrecao
SET @evento = (
    SELECT MIN(id_registro)
    FROM registro);

SELECT ri.id_registro_item AS id_item,
    ri.id_produto_setor AS id_produto,
    p.nome AS produto,
    ri.quantidade,
    ri.preco_unitario_praticado AS valor_unitario
FROM registroitem ri
JOIN produtosetor ps ON ps.id_produto_setor = ri.id_produto_setor
JOIN produto p ON p.id_produto = ps.id_produto
WHERE ri.id_registro = @evento
ORDER BY ri.id_registro_item;

-- 75. ConsultaOperacionalRepository.ItensParaCorrecao
SET @evento = (
    SELECT MIN(id_entrada)
    FROM entradamercadoria);

SELECT ei.id_entrada_item AS id_item,
    ei.id_produto,
    p.nome AS produto,
    ei.quantidade,
    ei.custo_unitario AS valor_unitario
FROM entradaitem ei
JOIN produto p ON p.id_produto = ei.id_produto
WHERE ei.id_entrada = @evento
ORDER BY ei.id_entrada_item;

-- 76. ConsultaOperacionalRepository.ItensParaCorrecao
SET @evento = (
    SELECT MIN(id_transferencia)
    FROM transferenciaestoque);

SELECT ti.id_transferencia_item AS id_item,
    ti.id_produto,
    p.nome AS produto,
    ti.quantidade,
    NULL AS valor_unitario
FROM transferenciaitem ti
JOIN produto p ON p.id_produto = ti.id_produto
WHERE ti.id_transferencia = @evento
ORDER BY ti.id_transferencia_item;

-- 77. ConsultaOperacionalRepository.HistoricoDaOperacao
SET @evento = (
    SELECT MIN(id_solicitacao_estoque_central)
    FROM solicitacaoestoquecentral);

SET @perfil = 'solicitacaoestoquecentral';

SELECT h.data_hora,
    u.login AS responsavel,
    h.justificativa,
    JSON_EXTRACT(h.dados_anteriores, '$.versao') AS versao_anterior,
    JSON_EXTRACT(h.dados_novos, '$.versao') AS versao_nova
FROM historicoalteracaooperacao h
JOIN usuario u ON u.id_usuario = h.id_usuario
WHERE h.entidade = @perfil
    AND h.id_entidade = @evento
ORDER BY h.id_historico DESC;

-- 78. TransferenciaEstoqueRepository.ListarPorLocalOrigem
SET @id = (
    SELECT MIN(id_transferencia)
    FROM transferenciaestoque);

SELECT id_transferencia,
    id_local_origem,
    id_local_destino,
    id_solicitacao,
    id_solicitacao_estoque_central,
    id_usuario_responsavel,
    data_hora,
    observacao,
    chave_operacao,
    criado_em,
    id_usuario_alteracao,
    atualizado_em,
    versao
FROM transferenciaestoque
WHERE id_local_origem = @id
ORDER BY data_hora DESC, id_transferencia DESC;

-- 79. TransferenciaEstoqueRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_transferencia)
    FROM transferenciaestoque);

SELECT id_transferencia,
    id_local_origem,
    id_local_destino,
    id_solicitacao,
    id_solicitacao_estoque_central,
    id_usuario_responsavel,
    data_hora,
    observacao,
    chave_operacao,
    criado_em,
    id_usuario_alteracao,
    atualizado_em,
    versao
FROM transferenciaestoque
WHERE id_transferencia = @id;

-- 80. TransferenciaItemRepository.ListarPorTransferencia
SET @id = (
    SELECT MIN(id_transferencia)
    FROM transferenciaestoque);

SELECT id_transferencia_item,
    id_transferencia,
    id_produto,
    quantidade
FROM transferenciaitem
WHERE id_transferencia = @id
ORDER BY id_transferencia_item;

-- 09 — Vendas e perdas
-- 81. ConsultaOperacionalRepository.ProdutosDaEscala
SET @evento = (
    SELECT MIN(id_escala)
    FROM escala);

SELECT ps.id_produto_setor,
    p.nome,
    ps.preco_venda
FROM escala e
JOIN jornadasetor j ON j.id_jornada = e.id_jornada
JOIN produtosetor ps ON ps.id_setor = j.id_setor
JOIN produto p ON p.id_produto = ps.id_produto
WHERE e.id_escala = @evento
    AND p.ativo = 1
ORDER BY p.nome;

-- 82. ConsultaOperacionalRepository.Registros
SET @evento = (
    SELECT MIN(id_evento)
    FROM evento);

SET @fim = DATE_ADD(CURDATE(), INTERVAL 2 DAY);

SET @inicio = DATE_SUB(CURDATE(), INTERVAL 31 DAY);

SET @perfil = 'Administrador';

SET @pessoa = (
    SELECT id_pessoa
    FROM usuario
    WHERE login = 'leandro');

SELECT r.id_registro,
    s.id_setor,
    j.id_jornada,
    r.data_hora,
    p.nome AS responsavel,
    s.nome AS setor,
    t.nome AS turno,
    r.tipo,
    pr.nome AS produto,
    ri.quantidade,
    ri.preco_unitario_praticado,
    r.forma_pagamento,
    r.observacao,
    ri.quantidade * ri.preco_unitario_praticado AS total
FROM registro r
JOIN escala e ON e.id_escala = r.id_escala
JOIN equipeevento eq ON eq.id_equipe = e.id_equipe
JOIN usuario u ON u.id_usuario = r.id_usuario_responsavel
JOIN pessoa p ON p.id_pessoa = u.id_pessoa
JOIN jornadasetor j ON j.id_jornada = e.id_jornada
JOIN turno t ON t.id_turno = j.id_turno
JOIN setor s ON s.id_setor = j.id_setor
JOIN evento ev ON ev.id_evento = s.id_evento
JOIN registroitem ri ON ri.id_registro = r.id_registro
JOIN produtosetor ps ON ps.id_produto_setor = ri.id_produto_setor
JOIN produto pr ON pr.id_produto = ps.id_produto
WHERE eq.id_evento = ev.id_evento
    AND ps.id_setor = s.id_setor
    AND (@perfil = 'Administrador'
        OR (@perfil = 'Gerente'
            AND ev.id_responsavel = @pessoa)
        OR (@perfil = 'Membro'
            AND eq.id_pessoa = @pessoa
            AND eq.ativo = 1))
    AND (@perfil <> 'Membro'
        OR u.id_pessoa = @pessoa)
    AND (@evento = 0
        OR ev.id_evento = @evento)
    AND r.data_hora >= @inicio
    AND r.data_hora < @fim
ORDER BY r.data_hora DESC, r.id_registro DESC;

-- 83. RegistroItemRepository.ListarPorRegistro
SET @id = (
    SELECT MIN(id_registro)
    FROM registro);

SELECT id_registro_item,
    id_registro,
    id_produto_setor,
    quantidade,
    preco_unitario_praticado
FROM registroitem
WHERE id_registro = @id
ORDER BY id_registro_item;

-- 84. RegistroRepository.ListarPorEscala
SET @id = (
    SELECT MIN(id_escala)
    FROM escala);

SELECT id_registro,
    id_escala,
    id_usuario_responsavel,
    tipo,
    forma_pagamento,
    data_hora,
    observacao,
    chave_operacao,
    criado_em,
    id_usuario_alteracao,
    atualizado_em,
    versao
FROM registro
WHERE id_escala = @id
ORDER BY data_hora DESC, id_registro DESC;

-- 85. RegistroRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_registro)
    FROM registro);

SELECT id_registro,
    id_escala,
    id_usuario_responsavel,
    tipo,
    forma_pagamento,
    data_hora,
    observacao,
    chave_operacao,
    criado_em,
    id_usuario_alteracao,
    atualizado_em,
    versao
FROM registro
WHERE id_registro = @id;

-- 10 — Despesas e conferência financeira
-- 86. ApuracaoRepository.VendasPorJornada
SET @jornada = (
    SELECT MIN(id_jornada)
    FROM jornadasetor);

SELECT r.id_registro,
    r.data_hora,
    p.nome AS responsavel,
    s.nome AS setor,
    t.nome AS turno,
    ri.quantidade,
    ri.preco_unitario_praticado,
    r.forma_pagamento,
    ri.quantidade * ri.preco_unitario_praticado AS total
FROM registro r
JOIN escala e ON e.id_escala = r.id_escala
JOIN jornadasetor j ON j.id_jornada = e.id_jornada
JOIN setor s ON s.id_setor = j.id_setor
JOIN turno t ON t.id_turno = j.id_turno
JOIN usuario u ON u.id_usuario = r.id_usuario_responsavel
JOIN pessoa p ON p.id_pessoa = u.id_pessoa
JOIN registroitem ri ON ri.id_registro = r.id_registro
WHERE j.id_jornada = @jornada
    AND r.tipo = 'venda'
ORDER BY r.data_hora, r.id_registro;

-- 87. ApuracaoRepository.ResumoPorJornada
SET @jornada = (
    SELECT MIN(id_jornada)
    FROM jornadasetor);

SELECT r.forma_pagamento,
    SUM(ri.quantidade * ri.preco_unitario_praticado) AS total,
    SUM(ri.preco_unitario_praticado IS NULL) AS itens_sem_preco
FROM registro r
JOIN escala e ON e.id_escala = r.id_escala
JOIN registroitem ri ON ri.id_registro = r.id_registro
WHERE e.id_jornada = @jornada
    AND r.tipo = 'venda'
GROUP BY r.forma_pagamento;

-- 88. ApuracaoRepository.ListarPorJornada
SET @id = (
    SELECT MIN(id_jornada)
    FROM jornadasetor);

SELECT id_apuracao,
    id_jornada,
    valor_informado,
    data_hora,
    id_usuario_responsavel,
    observacao,
    valor_dinheiro,
    valor_pix,
    valor_cartao
FROM apuracao
WHERE id_jornada = @id
ORDER BY id_apuracao;

-- 89. ApuracaoRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_apuracao)
    FROM apuracao);

SELECT id_apuracao,
    id_jornada,
    valor_informado,
    data_hora,
    id_usuario_responsavel,
    observacao,
    valor_dinheiro,
    valor_pix,
    valor_cartao
FROM apuracao
WHERE id_apuracao = @id;

-- 90. DespesaRepository.ListarPorEvento
SET @id = (
    SELECT MIN(id_evento)
    FROM evento);

SELECT id_despesa,
    id_evento,
    id_fornecedor,
    descricao,
    `data`,
    valor,
    id_usuario_responsavel
FROM despesa
WHERE id_evento = @id
ORDER BY `data` DESC, id_despesa DESC;

-- 91. DespesaRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_despesa)
    FROM despesa);

SELECT id_despesa,
    id_evento,
    id_fornecedor,
    descricao,
    `data`,
    valor,
    id_usuario_responsavel
FROM despesa
WHERE id_despesa = @id;

-- 11 — Relatórios
-- 92. ConsultaOperacionalRepository.Relatorio
SET @fim = DATE_ADD(CURDATE(), INTERVAL 2 DAY);

SET @inicio = DATE_SUB(CURDATE(), INTERVAL 31 DAY);

SET @perfil = 'Administrador';

SET @pessoa = (
    SELECT id_pessoa
    FROM usuario
    WHERE login = 'leandro');

SELECT ev.nome,
    ev.local,
    ev.data_inicio,
    ev.data_fim,
    ev.situacao,
    p.nome AS responsavel
FROM evento ev
JOIN pessoa p ON p.id_pessoa = ev.id_responsavel
WHERE (@perfil = 'Administrador'
        OR (@perfil = 'Gerente'
            AND ev.id_responsavel = @pessoa))
    AND ev.data_inicio < @fim
    AND ev.data_fim >= @inicio
ORDER BY ev.data_inicio, ev.nome;

-- 93. ConsultaOperacionalRepository.Relatorio
SET @fim = DATE_ADD(CURDATE(), INTERVAL 2 DAY);

SET @inicio = DATE_SUB(CURDATE(), INTERVAL 31 DAY);

SET @perfil = 'Administrador';

SET @pessoa = (
    SELECT id_pessoa
    FROM usuario
    WHERE login = 'leandro');

SELECT so.id_solicitacao,
    so.data_hora,
    ev.nome AS evento,
    s.nome AS setor,
    p.nome AS solicitante,
    so.situacao,
    pr.nome AS produto,
    si.quantidade,
    d.login AS decisor,
    so.decidida_em,
    so.justificativa_rejeicao
FROM solicitacao so
JOIN escala e ON e.id_escala = so.id_escala
JOIN equipeevento eq ON eq.id_equipe = e.id_equipe
JOIN pessoa p ON p.id_pessoa = eq.id_pessoa
JOIN jornadasetor j ON j.id_jornada = e.id_jornada
JOIN setor s ON s.id_setor = j.id_setor
JOIN evento ev ON ev.id_evento = s.id_evento
JOIN solicitacaoitem si ON si.id_solicitacao = so.id_solicitacao
JOIN produtosetor ps ON ps.id_produto_setor = si.id_produto_setor
JOIN produto pr ON pr.id_produto = ps.id_produto
LEFT JOIN usuario d ON d.id_usuario = so.id_usuario_decisor
WHERE eq.id_evento = ev.id_evento
    AND ps.id_setor = s.id_setor
    AND (@perfil = 'Administrador'
        OR (@perfil = 'Gerente'
            AND ev.id_responsavel = @pessoa)
        OR (@perfil = 'Membro'
            AND eq.id_pessoa = @pessoa
            AND eq.ativo = 1))
    AND so.data_hora >= @inicio
    AND so.data_hora < @fim
ORDER BY so.data_hora DESC;

-- 94. ConsultaOperacionalRepository.Relatorio
SET @fim = DATE_ADD(CURDATE(), INTERVAL 2 DAY);

SET @inicio = DATE_SUB(CURDATE(), INTERVAL 31 DAY);

SET @perfil = 'Administrador';

SET @pessoa = (
    SELECT id_pessoa
    FROM usuario
    WHERE login = 'leandro');

SELECT so.id_solicitacao_estoque_central,
    so.tipo,
    so.data_hora,
    ev.nome AS evento,
    u.login AS solicitante,
    so.situacao,
    p.nome AS produto,
    si.quantidade,
    d.login AS decisor,
    so.decidida_em,
    so.justificativa_rejeicao,
    tr.id_transferencia
FROM solicitacaoestoquecentral so
JOIN evento ev ON ev.id_evento = so.id_evento
JOIN usuario u ON u.id_usuario = so.id_usuario_solicitante
JOIN solicitacaoestoquecentralitem si ON si.id_solicitacao_estoque_central = so.id_solicitacao_estoque_central
JOIN produto p ON p.id_produto = si.id_produto
LEFT JOIN usuario d ON d.id_usuario = so.id_usuario_decisor
LEFT JOIN transferenciaestoque tr ON tr.id_solicitacao_estoque_central = so.id_solicitacao_estoque_central
WHERE (@perfil = 'Administrador'
        OR (@perfil = 'Gerente'
            AND ev.id_responsavel = @pessoa
            AND u.id_pessoa = @pessoa))
    AND so.data_hora >= @inicio
    AND so.data_hora < @fim
ORDER BY so.data_hora DESC;

-- 95. ConsultaOperacionalRepository.Relatorio
SET @fim = DATE_ADD(CURDATE(), INTERVAL 2 DAY);

SET @inicio = DATE_SUB(CURDATE(), INTERVAL 31 DAY);

SET @perfil = 'Administrador';

SET @pessoa = (
    SELECT id_pessoa
    FROM usuario
    WHERE login = 'leandro');

SELECT de.id_despesa,
    ev.nome AS evento,
    de.data,
    de.descricao,
    de.valor,
    f.nome AS fornecedor,
    u.login AS responsavel
FROM despesa de
JOIN evento ev ON ev.id_evento = de.id_evento
LEFT JOIN fornecedor f ON f.id_fornecedor = de.id_fornecedor
JOIN usuario u ON u.id_usuario = de.id_usuario_responsavel
WHERE (@perfil = 'Administrador'
        OR (@perfil = 'Gerente'
            AND ev.id_responsavel = @pessoa))
    AND de.data >= @inicio
    AND de.data < @fim
ORDER BY de.data DESC;

-- 96. ConsultaOperacionalRepository.Relatorio
SET @fim = DATE_ADD(CURDATE(), INTERVAL 2 DAY);

SET @inicio = DATE_SUB(CURDATE(), INTERVAL 31 DAY);

SET @perfil = 'Administrador';

SET @pessoa = (
    SELECT id_pessoa
    FROM usuario
    WHERE login = 'leandro');

SELECT a.id_apuracao,
    ev.nome AS evento,
    s.nome AS setor,
    j.data AS data_jornada,
    t.nome AS turno,
    a.data_hora,
    a.valor_informado,
    a.valor_dinheiro,
    a.valor_pix,
    a.valor_cartao,
    u.login AS responsavel,
    a.observacao,
    CASE WHEN COALESCE(v.pendentes, 0) > 0 THEN NULL ELSE COALESCE(v.total, 0) END AS esperado_atual,
    CASE WHEN COALESCE(v.pendentes, 0) > 0 THEN NULL ELSE a.valor_informado - COALESCE(v.total, 0) END AS diferenca_atual,
    CASE WHEN COALESCE(v.pendentes, 0) > 0 THEN NULL ELSE a.valor_dinheiro - COALESCE(v.dinheiro, 0) END AS diferenca_dinheiro_atual,
    CASE WHEN COALESCE(v.pendentes, 0) > 0 THEN NULL ELSE a.valor_pix - COALESCE(v.pix, 0) END AS diferenca_pix_atual,
    CASE WHEN COALESCE(v.pendentes, 0) > 0 THEN NULL ELSE a.valor_cartao - COALESCE(v.cartao, 0) END AS diferenca_cartao_atual
FROM apuracao a
JOIN jornadasetor j ON j.id_jornada = a.id_jornada
JOIN turno t ON t.id_turno = j.id_turno
JOIN setor s ON s.id_setor = j.id_setor
JOIN evento ev ON ev.id_evento = s.id_evento
JOIN usuario u ON u.id_usuario = a.id_usuario_responsavel
LEFT JOIN(
    SELECT e.id_jornada,
        SUM(ri.quantidade * ri.preco_unitario_praticado) AS total,
        SUM(CASE WHEN r.forma_pagamento = 'dinheiro' THEN ri.quantidade * ri.preco_unitario_praticado ELSE 0 END) AS dinheiro,
        SUM(CASE WHEN r.forma_pagamento = 'pix' THEN ri.quantidade * ri.preco_unitario_praticado ELSE 0 END) AS pix,
        SUM(CASE WHEN r.forma_pagamento = 'cartao' THEN ri.quantidade * ri.preco_unitario_praticado ELSE 0 END) AS cartao,
        SUM(ri.preco_unitario_praticado IS NULL
            OR r.forma_pagamento IS NULL
            OR r.forma_pagamento NOT IN ('dinheiro', 'pix', 'cartao')) AS pendentes
    FROM registro r
    JOIN escala e ON e.id_escala = r.id_escala
    JOIN registroitem ri ON ri.id_registro = r.id_registro
    WHERE r.tipo = 'venda'
    GROUP BY e.id_jornada) v ON v.id_jornada = a.id_jornada
WHERE (@perfil = 'Administrador'
        OR (@perfil = 'Gerente'
            AND ev.id_responsavel = @pessoa))
    AND j.data >= @inicio
    AND j.data < @fim
ORDER BY j.data DESC, a.data_hora DESC;

-- 97. ConsultaOperacionalRepository.InstituicoesDoRelatorio
SET @perfil = 'Administrador';

SET @pessoa = (
    SELECT id_pessoa
    FROM usuario
    WHERE login = 'leandro');

SELECT DISTINCT i.nome
FROM instituicao i
LEFT JOIN evento ev ON ev.id_instituicao = i.id_instituicao
WHERE @perfil = 'Administrador'
    OR (@perfil = 'Gerente'
        AND ev.id_responsavel = @pessoa)
    OR (@perfil = 'Membro'
        AND EXISTS (
        SELECT 1
        FROM equipeevento eq
        WHERE eq.id_evento = ev.id_evento
            AND eq.id_pessoa = @pessoa
            AND eq.ativo = 1))
ORDER BY i.nome;

-- 98. ProdutoSetorRepository.ListarPorSetor
SET @id = (
    SELECT MIN(id_setor)
    FROM estoquesetor);

SELECT id_produto_setor,
    id_setor,
    id_produto,
    preco_venda
FROM produtosetor
WHERE id_setor = @id
ORDER BY id_produto_setor;

-- 99. ProdutoSetorRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_produto_setor)
    FROM produtosetor);

SELECT id_produto_setor,
    id_setor,
    id_produto,
    preco_venda
FROM produtosetor
WHERE id_produto_setor = @id;
