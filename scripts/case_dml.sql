-- Cadastros e movimentações da Associação Comunitária Esperança.
-- Use um banco separado dos dados da organização.
-- As senhas são armazenadas somente como hash.
SET SESSION transaction_read_only = IF(COALESCE(@case_instalacao_bloqueada, 0)=1,
    1, @@session.transaction_read_only);
USE case_commercial;

SET NAMES utf8mb4 COLLATE utf8mb4_0900_ai_ci;

SET @data_evento = COALESCE((
        SELECT MIN(data_inicio)
        FROM evento
        WHERE nome = 'Festa da Comunidade'), DATE_SUB(CURDATE(), INTERVAL 1 DAY));

START TRANSACTION;

-- Instituição, perfis e contas de acesso.
INSERT INTO perfilsistema(nome)
SELECT 'Administrador'
WHERE NOT EXISTS (
    SELECT 1
    FROM perfilsistema
    WHERE nome = 'Administrador');

INSERT INTO perfilsistema(nome)
SELECT 'Gerente'
WHERE NOT EXISTS (
    SELECT 1
    FROM perfilsistema
    WHERE nome = 'Gerente');

INSERT INTO perfilsistema(nome)
SELECT 'Membro'
WHERE NOT EXISTS (
    SELECT 1
    FROM perfilsistema
    WHERE nome = 'Membro');

INSERT INTO perfil(nome)
SELECT 'Atendente'
WHERE NOT EXISTS (
    SELECT 1
    FROM perfil
    WHERE nome = 'Atendente');

INSERT INTO instituicao(nome,
        documento,
        telefone,
        email)
SELECT 'Associação Comunitária Esperança',
    NULL,
    NULL,
    'contato@esperanca.invalid'
WHERE NOT EXISTS (
    SELECT 1
    FROM instituicao
    WHERE nome = 'Associação Comunitária Esperança');

SET @instituicao = (
    SELECT id_instituicao
    FROM instituicao
    WHERE nome = 'Associação Comunitária Esperança');

INSERT INTO pessoa(nome,
        telefone,
        email)
SELECT 'Rafael Cayres',
    NULL,
    'rafael.cayres@esperanca.invalid'
WHERE NOT EXISTS (
    SELECT 1
    FROM pessoa
    WHERE email = 'rafael.cayres@esperanca.invalid');

INSERT INTO usuario(id_pessoa,
        id_perfil_sistema,
        login,
        senha_hash,
        situacao_conta,
        solicitado_em)
SELECT p.id_pessoa,
    ps.id_perfil_sistema,
    'rafael.cayres',
    'argon2id$v=19$m=65536,t=3,p=1$qpxjGypOeRLQsHhNpSAlvA==$92SpwZ8cUg8ojiYU7FUEE0K1MCx5P8I9ZlI0552Mw+A=',
    'aprovado',
    TIMESTAMP(@data_evento, '07:00:00')
FROM pessoa p
CROSS JOIN perfilsistema ps
WHERE p.email = 'rafael.cayres@esperanca.invalid'
    AND ps.nome = 'Administrador'
    AND NOT EXISTS (
    SELECT 1
    FROM usuario
    WHERE login = 'rafael.cayres');

SET @administrador = (
    SELECT id_usuario
    FROM usuario
    WHERE login = 'rafael.cayres');

INSERT INTO pessoa(nome,
        telefone,
        email)
SELECT 'Leandro',
    NULL,
    'leandro@esperanca.invalid'
WHERE NOT EXISTS (
    SELECT 1
    FROM pessoa
    WHERE email = 'leandro@esperanca.invalid');

INSERT INTO usuario(id_pessoa,
        id_perfil_sistema,
        login,
        senha_hash,
        situacao_conta,
        solicitado_em)
SELECT p.id_pessoa,
    ps.id_perfil_sistema,
    'leandro',
    'argon2id$v=19$m=65536,t=3,p=1$8fJux0WDSNXT5PGEH2oeZg==$usTiV3RbIaA9ZchaeVKGH4QxaTVSxjILYUvUVhAIyIg=',
    'aprovado',
    TIMESTAMP(@data_evento, '07:00:00')
FROM pessoa p
CROSS JOIN perfilsistema ps
WHERE p.email = 'leandro@esperanca.invalid'
    AND ps.nome = 'Gerente'
    AND NOT EXISTS (
    SELECT 1
    FROM usuario
    WHERE login = 'leandro');

SET @gerente = (
    SELECT id_usuario
    FROM usuario
    WHERE login = 'leandro');

INSERT INTO pessoa(nome,
        telefone,
        email)
SELECT 'Pedro Almeida',
    NULL,
    'pedro.almeida@esperanca.invalid'
WHERE NOT EXISTS (
    SELECT 1
    FROM pessoa
    WHERE email = 'pedro.almeida@esperanca.invalid');

INSERT INTO usuario(id_pessoa,
        id_perfil_sistema,
        login,
        senha_hash,
        situacao_conta,
        solicitado_em)
SELECT p.id_pessoa,
    ps.id_perfil_sistema,
    'pedro.almeida',
    'argon2id$v=19$m=65536,t=3,p=1$yb9JWgpxX0CziWfzzVKLzA==$qvSyAlQXxurF3MjY0pl2eXxfGy8Wf92+TCjnI8vqRJI=',
    'aprovado',
    TIMESTAMP(@data_evento, '07:00:00')
FROM pessoa p
CROSS JOIN perfilsistema ps
WHERE p.email = 'pedro.almeida@esperanca.invalid'
    AND ps.nome = 'Membro'
    AND NOT EXISTS (
    SELECT 1
    FROM usuario
    WHERE login = 'pedro.almeida');

SET @membro = (
    SELECT id_usuario
    FROM usuario
    WHERE login = 'pedro.almeida');

SET @pessoa_gerente = (
    SELECT id_pessoa
    FROM usuario
    WHERE id_usuario = @gerente);

SET @pessoa_membro = (
    SELECT id_pessoa
    FROM usuario
    WHERE id_usuario = @membro);

INSERT INTO tipoevento(nome)
SELECT 'Festa comunitária'
WHERE NOT EXISTS (
    SELECT 1
    FROM tipoevento
    WHERE nome = 'Festa comunitária');

INSERT INTO turno(nome)
SELECT 'Manhã'
WHERE NOT EXISTS (
    SELECT 1
    FROM turno
    WHERE nome = 'Manhã');

INSERT INTO turno(nome)
SELECT 'Tarde'
WHERE NOT EXISTS (
    SELECT 1
    FROM turno
    WHERE nome = 'Tarde');

INSERT INTO categoria(nome)
SELECT 'Bebidas'
WHERE NOT EXISTS (
    SELECT 1
    FROM categoria
    WHERE nome = 'Bebidas');

INSERT INTO categoria(nome)
SELECT 'Alimentos'
WHERE NOT EXISTS (
    SELECT 1
    FROM categoria
    WHERE nome = 'Alimentos');

INSERT INTO fornecedor(nome,
        documento,
        telefone,
        email)
SELECT 'Distribuidora Vale Verde',
    NULL,
    NULL,
    NULL
WHERE NOT EXISTS (
    SELECT 1
    FROM fornecedor
    WHERE nome = 'Distribuidora Vale Verde');

INSERT INTO fornecedor(nome,
        documento,
        telefone,
        email)
SELECT 'Padaria Pão da Vila',
    NULL,
    NULL,
    NULL
WHERE NOT EXISTS (
    SELECT 1
    FROM fornecedor
    WHERE nome = 'Padaria Pão da Vila');

SET @fornecedor_bebidas = (
    SELECT id_fornecedor
    FROM fornecedor
    WHERE nome = 'Distribuidora Vale Verde');

SET @fornecedor_lanches = (
    SELECT id_fornecedor
    FROM fornecedor
    WHERE nome = 'Padaria Pão da Vila');

INSERT INTO produto(id_categoria,
        nome,
        descricao,
        unidade_medida,
        ativo)
SELECT (
    SELECT id_categoria
    FROM categoria
    WHERE nome = 'Bebidas'),
    'Água mineral 500 ml',
    'Água mineral sem gás, garrafa de 500 ml.',
    'UN',
    1
WHERE NOT EXISTS (
    SELECT 1
    FROM produto
    WHERE nome = 'Água mineral 500 ml');

SET @agua = (
    SELECT id_produto
    FROM produto
    WHERE nome = 'Água mineral 500 ml');

INSERT INTO produtofornecedor(id_produto,
        id_fornecedor,
        codigo_no_fornecedor)
SELECT @agua,
    @fornecedor_bebidas,
    NULL
WHERE NOT EXISTS (
    SELECT 1
    FROM produtofornecedor
    WHERE id_produto = @agua
        AND id_fornecedor = @fornecedor_bebidas);

INSERT INTO produto(id_categoria,
        nome,
        descricao,
        unidade_medida,
        ativo)
SELECT (
    SELECT id_categoria
    FROM categoria
    WHERE nome = 'Bebidas'),
    'Refrigerante em lata',
    'Refrigerante em lata de 350 ml.',
    'UN',
    1
WHERE NOT EXISTS (
    SELECT 1
    FROM produto
    WHERE nome = 'Refrigerante em lata');

SET @refrigerante = (
    SELECT id_produto
    FROM produto
    WHERE nome = 'Refrigerante em lata');

INSERT INTO produtofornecedor(id_produto,
        id_fornecedor,
        codigo_no_fornecedor)
SELECT @refrigerante,
    @fornecedor_bebidas,
    NULL
WHERE NOT EXISTS (
    SELECT 1
    FROM produtofornecedor
    WHERE id_produto = @refrigerante
        AND id_fornecedor = @fornecedor_bebidas);

INSERT INTO produto(id_categoria,
        nome,
        descricao,
        unidade_medida,
        ativo)
SELECT (
    SELECT id_categoria
    FROM categoria
    WHERE nome = 'Alimentos'),
    'Salgado assado',
    'Salgado assado vendido por unidade.',
    'UN',
    1
WHERE NOT EXISTS (
    SELECT 1
    FROM produto
    WHERE nome = 'Salgado assado');

SET @salgado = (
    SELECT id_produto
    FROM produto
    WHERE nome = 'Salgado assado');

INSERT INTO produtofornecedor(id_produto,
        id_fornecedor,
        codigo_no_fornecedor)
SELECT @salgado,
    @fornecedor_lanches,
    NULL
WHERE NOT EXISTS (
    SELECT 1
    FROM produtofornecedor
    WHERE id_produto = @salgado
        AND id_fornecedor = @fornecedor_lanches);

-- Evento e locais de estoque.
INSERT INTO evento(id_instituicao,
        id_tipo_evento,
        nome,
        `local`,
        data_inicio,
        data_fim,
        id_responsavel,
        situacao,
        observacao,
        id_usuario_criacao,
        criado_em)
SELECT @instituicao,
    (
    SELECT id_tipo_evento
    FROM tipoevento
    WHERE nome = 'Festa comunitária'),
    'Festa da Comunidade',
    'Salão comunitário',
    @data_evento,
    DATE_ADD(@data_evento, INTERVAL 2 DAY),
    @pessoa_gerente,
    'em_andamento',
    'Arrecadação para manutenção das atividades comunitárias.',
    @gerente,
    TIMESTAMP(@data_evento, '07:15:00')
WHERE NOT EXISTS (
    SELECT 1
    FROM evento
    WHERE id_instituicao = @instituicao
        AND nome = 'Festa da Comunidade');

SET @evento = (
    SELECT id_evento
    FROM evento
    WHERE id_instituicao = @instituicao
        AND nome = 'Festa da Comunidade');

INSERT INTO localestoque(id_instituicao_central,
        nome,
        ativo,
        eh_geral_evento,
        id_evento)
SELECT @instituicao,
    'Estoque central',
    1,
    0,
    NULL
WHERE NOT EXISTS (
    SELECT 1
    FROM vw_localestoque
    WHERE id_instituicao = @instituicao
        AND eh_central = 1);

INSERT INTO localestoque(id_instituicao_central,
        nome,
        ativo,
        eh_geral_evento,
        id_evento)
SELECT NULL,
    'Estoque geral',
    1,
    1,
    @evento
WHERE NOT EXISTS (
    SELECT 1
    FROM vw_localestoque
    WHERE id_evento = @evento
        AND eh_geral_evento = 1);

SET @central = (
    SELECT id_local_estoque
    FROM vw_localestoque
    WHERE id_instituicao = @instituicao
        AND eh_central = 1);

SET @geral = (
    SELECT id_local_estoque
    FROM vw_localestoque
    WHERE id_evento = @evento
        AND eh_geral_evento = 1);

INSERT INTO setor(id_evento,
        nome)
SELECT @evento,
    'Barraca de Bebidas'
WHERE NOT EXISTS (
    SELECT 1
    FROM setor
    WHERE id_evento = @evento
        AND nome = 'Barraca de Bebidas');

SET @setor_bebidas = (
    SELECT id_setor
    FROM setor
    WHERE id_evento = @evento
        AND nome = 'Barraca de Bebidas');

INSERT INTO localestoque(id_instituicao_central,
        id_evento,
        nome,
        ativo,
        eh_geral_evento)
SELECT NULL,
    @evento,
    CONCAT('Estoque — ', 'Barraca de Bebidas'),
    1,
    0
WHERE NOT EXISTS (
    SELECT 1
    FROM vw_localestoque
    WHERE id_evento = @evento
        AND nome = CONCAT('Estoque — ', 'Barraca de Bebidas'));

SET @local_bebidas = (
    SELECT id_local_estoque
    FROM vw_localestoque
    WHERE id_evento = @evento
        AND nome = CONCAT('Estoque — ', 'Barraca de Bebidas'));

INSERT INTO estoquesetor(id_setor,
        id_local_estoque,
        id_evento)
SELECT @setor_bebidas,
    @local_bebidas,
    @evento
WHERE NOT EXISTS (
    SELECT 1
    FROM estoquesetor
    WHERE id_setor = @setor_bebidas);

INSERT INTO setor(id_evento,
        nome)
SELECT @evento,
    'Barraca de Lanches'
WHERE NOT EXISTS (
    SELECT 1
    FROM setor
    WHERE id_evento = @evento
        AND nome = 'Barraca de Lanches');

SET @setor_lanches = (
    SELECT id_setor
    FROM setor
    WHERE id_evento = @evento
        AND nome = 'Barraca de Lanches');

INSERT INTO localestoque(id_instituicao_central,
        id_evento,
        nome,
        ativo,
        eh_geral_evento)
SELECT NULL,
    @evento,
    CONCAT('Estoque — ', 'Barraca de Lanches'),
    1,
    0
WHERE NOT EXISTS (
    SELECT 1
    FROM vw_localestoque
    WHERE id_evento = @evento
        AND nome = CONCAT('Estoque — ', 'Barraca de Lanches'));

SET @local_lanches = (
    SELECT id_local_estoque
    FROM vw_localestoque
    WHERE id_evento = @evento
        AND nome = CONCAT('Estoque — ', 'Barraca de Lanches'));

INSERT INTO estoquesetor(id_setor,
        id_local_estoque,
        id_evento)
SELECT @setor_lanches,
    @local_lanches,
    @evento
WHERE NOT EXISTS (
    SELECT 1
    FROM estoquesetor
    WHERE id_setor = @setor_lanches);

-- Pedro trabalha em setores diferentes, sem sobreposição de horários.
INSERT INTO equipeevento(id_evento,
        id_pessoa,
        id_perfil,
        ativo,
        id_usuario_criacao,
        criado_em)
SELECT @evento,
    @pessoa_membro,
    (
    SELECT id_perfil
    FROM perfil
    WHERE nome = 'Atendente'),
    1,
    @gerente,
    TIMESTAMP(@data_evento, '07:20:00')
WHERE NOT EXISTS (
    SELECT 1
    FROM equipeevento
    WHERE id_evento = @evento
        AND id_pessoa = @pessoa_membro);

SET @equipe = (
    SELECT id_equipe
    FROM equipeevento
    WHERE id_evento = @evento
        AND id_pessoa = @pessoa_membro);

INSERT INTO jornadasetor(id_setor,
        `data`,
        id_turno)
SELECT @setor_bebidas,
    dias.data,
    (
    SELECT id_turno
    FROM turno
    WHERE nome = 'Manhã')
FROM(
    SELECT CAST(@data_evento AS DATE) AS data
    UNION ALL
    SELECT DATE_ADD(CAST(@data_evento AS DATE), INTERVAL 1 DAY)) dias
WHERE NOT EXISTS (
    SELECT 1
    FROM jornadasetor
    WHERE id_setor = @setor_bebidas
        AND `data` = dias.data
        AND id_turno = (
        SELECT id_turno
        FROM turno
        WHERE nome = 'Manhã'));

INSERT INTO escala(id_equipe,
        id_jornada,
        entrada_prevista,
        saida_prevista,
        situacao)
SELECT @equipe,
    j.id_jornada,
    '08:00:00',
    '12:00:00',
    'confirmada'
FROM jornadasetor j
WHERE j.id_setor = @setor_bebidas
    AND j.id_turno = (
    SELECT id_turno
    FROM turno
    WHERE nome = 'Manhã')
    AND NOT EXISTS (
    SELECT 1
    FROM escala
    WHERE id_equipe = @equipe
        AND id_jornada = j.id_jornada);

INSERT INTO jornadasetor(id_setor,
        `data`,
        id_turno)
SELECT @setor_lanches,
    dias.data,
    (
    SELECT id_turno
    FROM turno
    WHERE nome = 'Tarde')
FROM(
    SELECT CAST(@data_evento AS DATE) AS data
    UNION ALL
    SELECT DATE_ADD(CAST(@data_evento AS DATE), INTERVAL 1 DAY)) dias
WHERE NOT EXISTS (
    SELECT 1
    FROM jornadasetor
    WHERE id_setor = @setor_lanches
        AND `data` = dias.data
        AND id_turno = (
        SELECT id_turno
        FROM turno
        WHERE nome = 'Tarde'));

INSERT INTO escala(id_equipe,
        id_jornada,
        entrada_prevista,
        saida_prevista,
        situacao)
SELECT @equipe,
    j.id_jornada,
    '12:00:00',
    '18:00:00',
    'confirmada'
FROM jornadasetor j
WHERE j.id_setor = @setor_lanches
    AND j.id_turno = (
    SELECT id_turno
    FROM turno
    WHERE nome = 'Tarde')
    AND NOT EXISTS (
    SELECT 1
    FROM escala
    WHERE id_equipe = @equipe
        AND id_jornada = j.id_jornada);

SET @jornada_bebidas = (
    SELECT id_jornada
    FROM jornadasetor
    WHERE id_setor = @setor_bebidas
        AND `data` = @data_evento);

SET @escala_bebidas = (
    SELECT id_escala
    FROM escala
    WHERE id_equipe = @equipe
        AND id_jornada = @jornada_bebidas);

INSERT INTO produtosetor(id_setor,
        id_produto,
        preco_venda)
SELECT @setor_bebidas,
    @agua,
    5.00
WHERE NOT EXISTS (
    SELECT 1
    FROM produtosetor
    WHERE id_setor = @setor_bebidas
        AND id_produto = @agua);

INSERT INTO produtosetor(id_setor,
        id_produto,
        preco_venda)
SELECT @setor_bebidas,
    @refrigerante,
    7.00
WHERE NOT EXISTS (
    SELECT 1
    FROM produtosetor
    WHERE id_setor = @setor_bebidas
        AND id_produto = @refrigerante);

INSERT INTO produtosetor(id_setor,
        id_produto,
        preco_venda)
SELECT @setor_lanches,
    @salgado,
    8.00
WHERE NOT EXISTS (
    SELECT 1
    FROM produtosetor
    WHERE id_setor = @setor_lanches
        AND id_produto = @salgado);

-- Recebimentos externos antes da liberação ao evento.
INSERT INTO entradamercadoria(id_fornecedor,
        id_local_destino,
        id_usuario_responsavel,
        tipo_entrada,
        data_hora,
        numero_documento,
        observacao,
        chave_operacao,
        criado_em)
SELECT @fornecedor_bebidas,
    @central,
    @administrador,
    'compra',
    TIMESTAMP(@data_evento, '08:00:00'),
    CONCAT('REC-', DATE_FORMAT(@data_evento, '%Y%m%d'), '-001'),
    'Recebimento para a Festa da Comunidade.',
    UUID(),
    TIMESTAMP(@data_evento, '08:00:00')
WHERE NOT EXISTS (
    SELECT 1
    FROM entradamercadoria
    WHERE numero_documento = CONCAT('REC-', DATE_FORMAT(@data_evento, '%Y%m%d'), '-001')
        AND id_local_destino = @central);

SET @entrada_bebidas = (
    SELECT id_entrada
    FROM entradamercadoria
    WHERE numero_documento = CONCAT('REC-', DATE_FORMAT(@data_evento, '%Y%m%d'), '-001')
        AND id_local_destino = @central);

INSERT INTO entradamercadoria(id_fornecedor,
        id_local_destino,
        id_usuario_responsavel,
        tipo_entrada,
        data_hora,
        numero_documento,
        observacao,
        chave_operacao,
        criado_em)
SELECT @fornecedor_lanches,
    @central,
    @administrador,
    'compra',
    TIMESTAMP(@data_evento, '08:05:00'),
    CONCAT('REC-', DATE_FORMAT(@data_evento, '%Y%m%d'), '-002'),
    'Recebimento para a Festa da Comunidade.',
    UUID(),
    TIMESTAMP(@data_evento, '08:05:00')
WHERE NOT EXISTS (
    SELECT 1
    FROM entradamercadoria
    WHERE numero_documento = CONCAT('REC-', DATE_FORMAT(@data_evento, '%Y%m%d'), '-002')
        AND id_local_destino = @central);

SET @entrada_lanches = (
    SELECT id_entrada
    FROM entradamercadoria
    WHERE numero_documento = CONCAT('REC-', DATE_FORMAT(@data_evento, '%Y%m%d'), '-002')
        AND id_local_destino = @central);

INSERT INTO entradaitem(id_entrada,
        id_produto,
        quantidade,
        custo_unitario)
SELECT @entrada_bebidas,
    @agua,
    120,
    1.50
WHERE NOT EXISTS (
    SELECT 1
    FROM entradaitem
    WHERE id_entrada = @entrada_bebidas
        AND id_produto = @agua);

INSERT INTO entradaitem(id_entrada,
        id_produto,
        quantidade,
        custo_unitario)
SELECT @entrada_bebidas,
    @refrigerante,
    80,
    2.50
WHERE NOT EXISTS (
    SELECT 1
    FROM entradaitem
    WHERE id_entrada = @entrada_bebidas
        AND id_produto = @refrigerante);

INSERT INTO entradaitem(id_entrada,
        id_produto,
        quantidade,
        custo_unitario)
SELECT @entrada_lanches,
    @salgado,
    50,
    3.00
WHERE NOT EXISTS (
    SELECT 1
    FROM entradaitem
    WHERE id_entrada = @entrada_lanches
        AND id_produto = @salgado);

-- Pedido aprovado às 08:45; atendimento integral às 09:00.
INSERT INTO solicitacaoestoquecentral(id_local_origem,
        id_local_destino,
        id_usuario_solicitante,
        data_hora,
        situacao,
        id_usuario_decisor,
        decidida_em,
        versao)
SELECT @central,
    @geral,
    @gerente,
    TIMESTAMP(@data_evento, '08:30:00'),
    'atendida',
    @administrador,
    TIMESTAMP(@data_evento, '08:45:00'),
    3
WHERE NOT EXISTS (
    SELECT 1
    FROM vw_solicitacaoestoquecentral
    WHERE id_evento = @evento
        AND tipo = 'liberacao'
        AND data_hora = TIMESTAMP(@data_evento, '08:30:00'));

SET @pedido = (
    SELECT id_solicitacao_estoque_central
    FROM vw_solicitacaoestoquecentral
    WHERE id_evento = @evento
        AND data_hora = TIMESTAMP(@data_evento, '08:30:00'));

INSERT INTO solicitacaoestoquecentralitem(id_solicitacao_estoque_central,
        id_produto,
        quantidade)
SELECT @pedido,
    @agua,
    30
WHERE NOT EXISTS (
    SELECT 1
    FROM solicitacaoestoquecentralitem
    WHERE id_solicitacao_estoque_central = @pedido
        AND id_produto = @agua);

INSERT INTO solicitacaoestoquecentralitem(id_solicitacao_estoque_central,
        id_produto,
        quantidade)
SELECT @pedido,
    @refrigerante,
    20
WHERE NOT EXISTS (
    SELECT 1
    FROM solicitacaoestoquecentralitem
    WHERE id_solicitacao_estoque_central = @pedido
        AND id_produto = @refrigerante);

INSERT INTO solicitacaoestoquecentralitem(id_solicitacao_estoque_central,
        id_produto,
        quantidade)
SELECT @pedido,
    @salgado,
    10
WHERE NOT EXISTS (
    SELECT 1
    FROM solicitacaoestoquecentralitem
    WHERE id_solicitacao_estoque_central = @pedido
        AND id_produto = @salgado);

INSERT INTO transferenciaestoque(id_local_origem,
        id_local_destino,
        id_solicitacao_estoque_central,
        id_usuario_responsavel,
        data_hora,
        observacao,
        chave_operacao,
        criado_em)
SELECT @central,
    @geral,
    @pedido,
    @administrador,
    TIMESTAMP(@data_evento, '09:00:00'),
    'Liberação integral para a Festa da Comunidade.',
    UUID(),
    TIMESTAMP(@data_evento, '09:00:00')
WHERE NOT EXISTS (
    SELECT 1
    FROM transferenciaestoque
    WHERE id_solicitacao_estoque_central = @pedido);

SET @transferencia_central = (
    SELECT id_transferencia
    FROM transferenciaestoque
    WHERE id_solicitacao_estoque_central = @pedido);

INSERT INTO transferenciaitem(id_transferencia,
        id_produto,
        quantidade)
SELECT @transferencia_central,
    si.id_produto,
    si.quantidade
FROM solicitacaoestoquecentralitem si
WHERE si.id_solicitacao_estoque_central = @pedido
    AND NOT EXISTS (
    SELECT 1
    FROM transferenciaitem
    WHERE id_transferencia = @transferencia_central
        AND id_produto = si.id_produto);

INSERT INTO transferenciaestoque(id_local_origem,
        id_local_destino,
        id_usuario_responsavel,
        data_hora,
        observacao,
        chave_operacao,
        criado_em)
SELECT @geral,
    @local_bebidas,
    @gerente,
    TIMESTAMP(@data_evento, '09:15:00'),
    'Distribuição inicial para a barraca.',
    UUID(),
    TIMESTAMP(@data_evento, '09:15:00')
WHERE NOT EXISTS (
    SELECT 1
    FROM transferenciaestoque
    WHERE id_local_origem = @geral
        AND id_local_destino = @local_bebidas
        AND data_hora = TIMESTAMP(@data_evento, '09:15:00'));

SET @distribuicao_bebidas = (
    SELECT id_transferencia
    FROM transferenciaestoque
    WHERE id_local_origem = @geral
        AND id_local_destino = @local_bebidas
        AND data_hora = TIMESTAMP(@data_evento, '09:15:00'));

INSERT INTO transferenciaestoque(id_local_origem,
        id_local_destino,
        id_usuario_responsavel,
        data_hora,
        observacao,
        chave_operacao,
        criado_em)
SELECT @geral,
    @local_lanches,
    @gerente,
    TIMESTAMP(@data_evento, '09:20:00'),
    'Distribuição inicial para a barraca.',
    UUID(),
    TIMESTAMP(@data_evento, '09:20:00')
WHERE NOT EXISTS (
    SELECT 1
    FROM transferenciaestoque
    WHERE id_local_origem = @geral
        AND id_local_destino = @local_lanches
        AND data_hora = TIMESTAMP(@data_evento, '09:20:00'));

SET @distribuicao_lanches = (
    SELECT id_transferencia
    FROM transferenciaestoque
    WHERE id_local_origem = @geral
        AND id_local_destino = @local_lanches
        AND data_hora = TIMESTAMP(@data_evento, '09:20:00'));

INSERT INTO transferenciaitem(id_transferencia,
        id_produto,
        quantidade)
SELECT @distribuicao_bebidas,
    @agua,
    20
WHERE NOT EXISTS (
    SELECT 1
    FROM transferenciaitem
    WHERE id_transferencia = @distribuicao_bebidas
        AND id_produto = @agua);

INSERT INTO transferenciaitem(id_transferencia,
        id_produto,
        quantidade)
SELECT @distribuicao_bebidas,
    @refrigerante,
    12
WHERE NOT EXISTS (
    SELECT 1
    FROM transferenciaitem
    WHERE id_transferencia = @distribuicao_bebidas
        AND id_produto = @refrigerante);

INSERT INTO transferenciaitem(id_transferencia,
        id_produto,
        quantidade)
SELECT @distribuicao_lanches,
    @salgado,
    8
WHERE NOT EXISTS (
    SELECT 1
    FROM transferenciaitem
    WHERE id_transferencia = @distribuicao_lanches
        AND id_produto = @salgado);

-- Vendas por modalidade e uma perda justificada no setor de bebidas.
INSERT INTO registro(id_escala,
        id_usuario_responsavel,
        tipo,
        forma_pagamento,
        data_hora,
        observacao,
        chave_operacao,
        criado_em)
SELECT @escala_bebidas,
    @membro,
    'venda',
    'dinheiro',
    TIMESTAMP(@data_evento, '10:00:00'),
    'Venda de água no balcão.',
    UUID(),
    TIMESTAMP(@data_evento, '10:00:00')
WHERE NOT EXISTS (
    SELECT 1
    FROM registro
    WHERE id_escala = @escala_bebidas
        AND data_hora = TIMESTAMP(@data_evento, '10:00:00'));

SET @registro_dinheiro = (
    SELECT id_registro
    FROM registro
    WHERE id_escala = @escala_bebidas
        AND data_hora = TIMESTAMP(@data_evento, '10:00:00'));

INSERT INTO registroitem(id_registro,
        id_produto_setor,
        quantidade,
        preco_unitario_praticado)
SELECT @registro_dinheiro,
    (
    SELECT id_produto_setor
    FROM produtosetor
    WHERE id_setor = @setor_bebidas
        AND id_produto = @agua),
    2,
    5.00
WHERE NOT EXISTS (
    SELECT 1
    FROM registroitem
    WHERE id_registro = @registro_dinheiro);

INSERT INTO registro(id_escala,
        id_usuario_responsavel,
        tipo,
        forma_pagamento,
        data_hora,
        observacao,
        chave_operacao,
        criado_em)
SELECT @escala_bebidas,
    @membro,
    'venda',
    'pix',
    TIMESTAMP(@data_evento, '10:15:00'),
    'Venda de refrigerantes no balcão.',
    UUID(),
    TIMESTAMP(@data_evento, '10:15:00')
WHERE NOT EXISTS (
    SELECT 1
    FROM registro
    WHERE id_escala = @escala_bebidas
        AND data_hora = TIMESTAMP(@data_evento, '10:15:00'));

SET @registro_pix = (
    SELECT id_registro
    FROM registro
    WHERE id_escala = @escala_bebidas
        AND data_hora = TIMESTAMP(@data_evento, '10:15:00'));

INSERT INTO registroitem(id_registro,
        id_produto_setor,
        quantidade,
        preco_unitario_praticado)
SELECT @registro_pix,
    (
    SELECT id_produto_setor
    FROM produtosetor
    WHERE id_setor = @setor_bebidas
        AND id_produto = @refrigerante),
    2,
    7.00
WHERE NOT EXISTS (
    SELECT 1
    FROM registroitem
    WHERE id_registro = @registro_pix);

INSERT INTO registro(id_escala,
        id_usuario_responsavel,
        tipo,
        forma_pagamento,
        data_hora,
        observacao,
        chave_operacao,
        criado_em)
SELECT @escala_bebidas,
    @membro,
    'venda',
    'cartao',
    TIMESTAMP(@data_evento, '10:30:00'),
    'Venda de água com pagamento em cartão.',
    UUID(),
    TIMESTAMP(@data_evento, '10:30:00')
WHERE NOT EXISTS (
    SELECT 1
    FROM registro
    WHERE id_escala = @escala_bebidas
        AND data_hora = TIMESTAMP(@data_evento, '10:30:00'));

SET @registro_cartao = (
    SELECT id_registro
    FROM registro
    WHERE id_escala = @escala_bebidas
        AND data_hora = TIMESTAMP(@data_evento, '10:30:00'));

INSERT INTO registroitem(id_registro,
        id_produto_setor,
        quantidade,
        preco_unitario_praticado)
SELECT @registro_cartao,
    (
    SELECT id_produto_setor
    FROM produtosetor
    WHERE id_setor = @setor_bebidas
        AND id_produto = @agua),
    1,
    5.00
WHERE NOT EXISTS (
    SELECT 1
    FROM registroitem
    WHERE id_registro = @registro_cartao);

INSERT INTO registro(id_escala,
        id_usuario_responsavel,
        tipo,
        forma_pagamento,
        data_hora,
        observacao,
        chave_operacao,
        criado_em)
SELECT @escala_bebidas,
    @membro,
    'perda',
    NULL,
    TIMESTAMP(@data_evento, '10:45:00'),
    'Garrafa danificada durante o transporte para o balcão.',
    UUID(),
    TIMESTAMP(@data_evento, '10:45:00')
WHERE NOT EXISTS (
    SELECT 1
    FROM registro
    WHERE id_escala = @escala_bebidas
        AND data_hora = TIMESTAMP(@data_evento, '10:45:00'));

SET @registro_perda = (
    SELECT id_registro
    FROM registro
    WHERE id_escala = @escala_bebidas
        AND data_hora = TIMESTAMP(@data_evento, '10:45:00'));

INSERT INTO registroitem(id_registro,
        id_produto_setor,
        quantidade,
        preco_unitario_praticado)
SELECT @registro_perda,
    (
    SELECT id_produto_setor
    FROM produtosetor
    WHERE id_setor = @setor_bebidas
        AND id_produto = @agua),
    1,
    NULL
WHERE NOT EXISTS (
    SELECT 1
    FROM registroitem
    WHERE id_registro = @registro_perda);

INSERT INTO solicitacao(id_escala,
        tipo,
        descricao,
        data_hora,
        situacao,
        id_usuario_solicitante,
        criado_em)
SELECT @escala_bebidas,
    'reposicao',
    'Reposição de água para continuidade do atendimento.',
    TIMESTAMP(@data_evento, '11:00:00'),
    'pendente',
    @membro,
    TIMESTAMP(@data_evento, '11:00:00')
WHERE NOT EXISTS (
    SELECT 1
    FROM solicitacao
    WHERE id_escala = @escala_bebidas
        AND data_hora = TIMESTAMP(@data_evento, '11:00:00'));

SET @reposicao = (
    SELECT id_solicitacao
    FROM solicitacao
    WHERE id_escala = @escala_bebidas
        AND data_hora = TIMESTAMP(@data_evento, '11:00:00'));

INSERT INTO solicitacaoitem(id_solicitacao,
        id_produto_setor,
        quantidade)
SELECT @reposicao,
    (
    SELECT id_produto_setor
    FROM produtosetor
    WHERE id_setor = @setor_bebidas
        AND id_produto = @agua),
    3
WHERE NOT EXISTS (
    SELECT 1
    FROM solicitacaoitem
    WHERE id_solicitacao = @reposicao);

-- Uma aprovação aguarda entrega; o pedido posterior ainda aguarda análise.
INSERT INTO solicitacaoestoquecentral(id_local_origem,
        id_local_destino,
        id_usuario_solicitante,
        data_hora,
        situacao,
        id_usuario_decisor,
        decidida_em,
        versao)
SELECT @central,
    @geral,
    @gerente,
    TIMESTAMP(@data_evento, '11:30:00'),
    'aprovada',
    @administrador,
    TIMESTAMP(@data_evento, '11:45:00'),
    2
WHERE NOT EXISTS (
    SELECT 1
    FROM vw_solicitacaoestoquecentral
    WHERE id_evento = @evento
        AND data_hora = TIMESTAMP(@data_evento, '11:30:00'));

SET @pedido_aprovado = (
    SELECT id_solicitacao_estoque_central
    FROM vw_solicitacaoestoquecentral
    WHERE id_evento = @evento
        AND data_hora = TIMESTAMP(@data_evento, '11:30:00'));

INSERT INTO solicitacaoestoquecentralitem(id_solicitacao_estoque_central,
        id_produto,
        quantidade)
SELECT @pedido_aprovado,
    @agua,
    5
WHERE NOT EXISTS (
    SELECT 1
    FROM solicitacaoestoquecentralitem
    WHERE id_solicitacao_estoque_central = @pedido_aprovado
        AND id_produto = @agua);

INSERT INTO solicitacaoestoquecentral(id_local_origem,
        id_local_destino,
        id_usuario_solicitante,
        data_hora,
        situacao)
SELECT @central,
    @geral,
    @gerente,
    TIMESTAMP(@data_evento, '12:00:00'),
    'pendente'
WHERE NOT EXISTS (
    SELECT 1
    FROM vw_solicitacaoestoquecentral
    WHERE id_evento = @evento
        AND data_hora = TIMESTAMP(@data_evento, '12:00:00'));

SET @pedido_pendente = (
    SELECT id_solicitacao_estoque_central
    FROM vw_solicitacaoestoquecentral
    WHERE id_evento = @evento
        AND data_hora = TIMESTAMP(@data_evento, '12:00:00'));

INSERT INTO solicitacaoestoquecentralitem(id_solicitacao_estoque_central,
        id_produto,
        quantidade)
SELECT @pedido_pendente,
    @refrigerante,
    6
WHERE NOT EXISTS (
    SELECT 1
    FROM solicitacaoestoquecentralitem
    WHERE id_solicitacao_estoque_central = @pedido_pendente
        AND id_produto = @refrigerante);

INSERT INTO despesa(id_evento,
        id_fornecedor,
        descricao,
        `data`,
        valor,
        id_usuario_responsavel)
SELECT @evento,
    @fornecedor_bebidas,
    'Gelo para conservação das bebidas',
    @data_evento,
    20.00,
    @gerente
WHERE NOT EXISTS (
    SELECT 1
    FROM despesa
    WHERE id_evento = @evento
        AND descricao = 'Gelo para conservação das bebidas'
        AND `data` = @data_evento);

INSERT INTO apuracao(id_jornada,
        data_hora,
        id_usuario_responsavel,
        observacao,
        valor_dinheiro,
        valor_pix,
        valor_cartao)
SELECT @jornada_bebidas,
    TIMESTAMP(@data_evento, '12:30:00'),
    @gerente,
    'Conferência das vendas do turno da manhã.',
    10.00,
    14.00,
    5.00
WHERE NOT EXISTS (
    SELECT 1
    FROM vw_apuracao
    WHERE id_jornada = @jornada_bebidas);

-- Aprovação e atendimento preservam etapas e horários distintos.
INSERT INTO historicoalteracaooperacao(entidade,
        id_entidade,
        id_usuario,
        data_hora,
        justificativa,
        dados_anteriores,
        dados_novos)
SELECT 'solicitacaoestoquecentral',
    @pedido,
    @administrador,
    TIMESTAMP(@data_evento, '08:45:00'),
    'Aprovação da liberação para o evento.',
    JSON_OBJECT('situacao', 'pendente'),
    JSON_OBJECT('situacao', 'aprovada')
WHERE NOT EXISTS (
    SELECT 1
    FROM historicoalteracaooperacao
    WHERE entidade = 'solicitacaoestoquecentral'
        AND id_entidade = @pedido
        AND justificativa = 'Aprovação da liberação para o evento.');

INSERT INTO historicoalteracaooperacao(entidade,
        id_entidade,
        id_usuario,
        data_hora,
        justificativa,
        dados_anteriores,
        dados_novos)
SELECT 'solicitacaoestoquecentral',
    @pedido,
    @administrador,
    TIMESTAMP(@data_evento, '09:00:00'),
    'Entrega integral recebida no estoque geral.',
    JSON_OBJECT('situacao', 'aprovada'),
    JSON_OBJECT('situacao', 'atendida', 'id_transferencia', @transferencia_central)
WHERE NOT EXISTS (
    SELECT 1
    FROM historicoalteracaooperacao
    WHERE entidade = 'solicitacaoestoquecentral'
        AND id_entidade = @pedido
        AND justificativa = 'Entrega integral recebida no estoque geral.');

INSERT INTO historicoalteracaooperacao(entidade,
        id_entidade,
        id_usuario,
        data_hora,
        justificativa,
        dados_anteriores,
        dados_novos)
SELECT 'solicitacaoestoquecentral',
    @pedido_aprovado,
    @administrador,
    TIMESTAMP(@data_evento, '11:45:00'),
    'Aprovação de reposição para o estoque geral.',
    JSON_OBJECT('situacao', 'pendente'),
    JSON_OBJECT('situacao', 'aprovada')
WHERE NOT EXISTS (
    SELECT 1
    FROM historicoalteracaooperacao
    WHERE entidade = 'solicitacaoestoquecentral'
        AND id_entidade = @pedido_aprovado
        AND justificativa = 'Aprovação de reposição para o estoque geral.');

-- Saldo por produto e local, calculado a partir das operações acima.
INSERT INTO estoque(id_local_estoque,
        id_produto,
        quantidade_minima,
        quantidade_ideal,
        quantidade_atual)
SELECT movimentos.local_id,
    movimentos.produto_id,
    CASE WHEN l.eh_central = 1 THEN 10 ELSE 3 END,
    CASE WHEN l.eh_central = 1 THEN 150 ELSE 30 END,
    SUM(movimentos.quantidade)
FROM(
    SELECT en.id_local_destino AS local_id,
        ei.id_produto AS produto_id,
        ei.quantidade
    FROM entradamercadoria en
    JOIN entradaitem ei ON ei.id_entrada = en.id_entrada
    UNION ALL
    SELECT t.id_local_destino,
        ti.id_produto,
        ti.quantidade
    FROM transferenciaestoque t
    JOIN transferenciaitem ti ON ti.id_transferencia = t.id_transferencia
    UNION ALL
    SELECT t.id_local_origem,
        ti.id_produto,
        - ti.quantidade
    FROM transferenciaestoque t
    JOIN transferenciaitem ti ON ti.id_transferencia = t.id_transferencia
    UNION ALL
    SELECT es.id_local_estoque,
        ps.id_produto,
        - ri.quantidade
    FROM registroitem ri
    JOIN produtosetor ps ON ps.id_produto_setor = ri.id_produto_setor
    JOIN estoquesetor es ON es.id_setor = ps.id_setor) movimentos
JOIN vw_localestoque l ON l.id_local_estoque = movimentos.local_id
WHERE l.id_instituicao = @instituicao
    AND NOT EXISTS (
    SELECT 1
    FROM estoque e
    WHERE e.id_local_estoque = movimentos.local_id
        AND e.id_produto = movimentos.produto_id)
GROUP BY movimentos.local_id, movimentos.produto_id, l.eh_central;

COMMIT;
