-- Estrutura do banco de dados do CASE.
-- Cria um banco novo, sem apagar um banco existente.
-- Se o banco já existir, pare no erro de criação. A sessão fica somente leitura se o cliente continuar após o erro.
USE information_schema;
SET SESSION transaction_read_only = 0;
CREATE DATABASE case_commercial CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
GET DIAGNOSTICS @case_erros_criacao = NUMBER;
SET @case_instalacao_bloqueada = (@case_erros_criacao <> 0);
SET SESSION transaction_read_only = @case_instalacao_bloqueada;
USE case_commercial;
SET NAMES utf8mb4 COLLATE utf8mb4_0900_ai_ci;
-- Tabelas, chaves, índices e restrições do CASE.
SET FOREIGN_KEY_CHECKS=0;
CREATE TABLE `analiseusuario` (
  `id_analise` int NOT NULL AUTO_INCREMENT,
  `id_usuario` int NOT NULL,
  `id_analisador` int NOT NULL,
  `decisao` varchar(30) NOT NULL,
  `data_hora` datetime NOT NULL,
  `justificativa` text,
  PRIMARY KEY (`id_analise`),
  KEY `id_usuario` (`id_usuario`),
  KEY `id_analisador` (`id_analisador`),
  CONSTRAINT `analiseusuario_ibfk_1` FOREIGN KEY (`id_usuario`) REFERENCES `usuario` (`id_usuario`),
  CONSTRAINT `analiseusuario_ibfk_2` FOREIGN KEY (`id_analisador`) REFERENCES `usuario` (`id_usuario`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `apuracao` (
  `id_apuracao` int NOT NULL AUTO_INCREMENT,
  `id_jornada` int NOT NULL,
  `data_hora` datetime NOT NULL,
  `id_usuario_responsavel` int NOT NULL,
  `observacao` text,
  `valor_dinheiro` decimal(10,2) DEFAULT NULL,
  `valor_pix` decimal(10,2) DEFAULT NULL,
  `valor_cartao` decimal(10,2) DEFAULT NULL,
  PRIMARY KEY (`id_apuracao`),
  KEY `id_jornada` (`id_jornada`),
  KEY `id_usuario_responsavel` (`id_usuario_responsavel`),
  CONSTRAINT `apuracao_ibfk_1` FOREIGN KEY (`id_jornada`) REFERENCES `jornadasetor` (`id_jornada`),
  CONSTRAINT `apuracao_ibfk_2` FOREIGN KEY (`id_usuario_responsavel`) REFERENCES `usuario` (`id_usuario`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `categoria` (
  `id_categoria` int NOT NULL AUTO_INCREMENT,
  `nome` varchar(100) NOT NULL,
  PRIMARY KEY (`id_categoria`),
  UNIQUE KEY `nome` (`nome`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `despesa` (
  `id_despesa` int NOT NULL AUTO_INCREMENT,
  `id_evento` int NOT NULL,
  `id_fornecedor` int DEFAULT NULL,
  `descricao` varchar(255) NOT NULL,
  `data` date NOT NULL,
  `valor` decimal(10,2) NOT NULL,
  `id_usuario_responsavel` int NOT NULL,
  PRIMARY KEY (`id_despesa`),
  KEY `id_evento` (`id_evento`),
  KEY `id_fornecedor` (`id_fornecedor`),
  KEY `id_usuario_responsavel` (`id_usuario_responsavel`),
  CONSTRAINT `despesa_ibfk_1` FOREIGN KEY (`id_evento`) REFERENCES `evento` (`id_evento`),
  CONSTRAINT `despesa_ibfk_2` FOREIGN KEY (`id_fornecedor`) REFERENCES `fornecedor` (`id_fornecedor`),
  CONSTRAINT `despesa_ibfk_3` FOREIGN KEY (`id_usuario_responsavel`) REFERENCES `usuario` (`id_usuario`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `entradaitem` (
  `id_entrada_item` int NOT NULL AUTO_INCREMENT,
  `id_entrada` int NOT NULL,
  `id_produto` int NOT NULL,
  `quantidade` decimal(10,3) NOT NULL,
  `custo_unitario` decimal(10,2) DEFAULT NULL,
  PRIMARY KEY (`id_entrada_item`),
  KEY `id_entrada` (`id_entrada`),
  KEY `id_produto` (`id_produto`),
  CONSTRAINT `entradaitem_ibfk_1` FOREIGN KEY (`id_entrada`) REFERENCES `entradamercadoria` (`id_entrada`),
  CONSTRAINT `entradaitem_ibfk_2` FOREIGN KEY (`id_produto`) REFERENCES `produto` (`id_produto`),
  CONSTRAINT `chk_entradaitem_quantidade` CHECK (((`quantidade` > 0) and (`quantidade` = floor(`quantidade`))))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `entradamercadoria` (
  `id_entrada` int NOT NULL AUTO_INCREMENT,
  `id_fornecedor` int DEFAULT NULL,
  `id_local_destino` int NOT NULL,
  `id_usuario_responsavel` int NOT NULL,
  `tipo_entrada` varchar(30) NOT NULL,
  `data_hora` datetime NOT NULL,
  `numero_documento` varchar(50) DEFAULT NULL,
  `observacao` text,
  `chave_operacao` char(36) DEFAULT NULL,
  `criado_em` datetime DEFAULT NULL,
  `id_usuario_alteracao` int DEFAULT NULL,
  `atualizado_em` datetime DEFAULT NULL,
  `versao` int unsigned NOT NULL DEFAULT '1',
  PRIMARY KEY (`id_entrada`),
  UNIQUE KEY `uq_entrada_chave_operacao` (`chave_operacao`),
  KEY `id_fornecedor` (`id_fornecedor`),
  KEY `id_local_destino` (`id_local_destino`),
  KEY `id_usuario_responsavel` (`id_usuario_responsavel`),
  KEY `ix_entrada_usuario_alteracao` (`id_usuario_alteracao`),
  CONSTRAINT `entradamercadoria_ibfk_1` FOREIGN KEY (`id_fornecedor`) REFERENCES `fornecedor` (`id_fornecedor`),
  CONSTRAINT `entradamercadoria_ibfk_2` FOREIGN KEY (`id_local_destino`) REFERENCES `localestoque` (`id_local_estoque`),
  CONSTRAINT `entradamercadoria_ibfk_3` FOREIGN KEY (`id_usuario_responsavel`) REFERENCES `usuario` (`id_usuario`),
  CONSTRAINT `fk_entrada_usuario_alteracao` FOREIGN KEY (`id_usuario_alteracao`) REFERENCES `usuario` (`id_usuario`),
  CONSTRAINT `chk_entradamercadoria_tipo` CHECK ((lower(`tipo_entrada`) in (_cp850'compra',_cp850'doacao')))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `equipeevento` (
  `id_equipe` int NOT NULL AUTO_INCREMENT,
  `id_evento` int NOT NULL,
  `id_pessoa` int NOT NULL,
  `id_perfil` int NOT NULL,
  `ativo` tinyint(1) NOT NULL,
  `id_usuario_criacao` int DEFAULT NULL,
  `criado_em` datetime DEFAULT NULL,
  `id_usuario_alteracao` int DEFAULT NULL,
  `atualizado_em` datetime DEFAULT NULL,
  `versao` int unsigned NOT NULL DEFAULT '1',
  PRIMARY KEY (`id_equipe`),
  UNIQUE KEY `id_evento` (`id_evento`,`id_pessoa`),
  KEY `id_pessoa` (`id_pessoa`),
  KEY `id_perfil` (`id_perfil`),
  KEY `ix_equipeevento_usuario_criacao` (`id_usuario_criacao`),
  KEY `ix_equipeevento_usuario_alteracao` (`id_usuario_alteracao`),
  CONSTRAINT `equipeevento_ibfk_1` FOREIGN KEY (`id_evento`) REFERENCES `evento` (`id_evento`),
  CONSTRAINT `equipeevento_ibfk_2` FOREIGN KEY (`id_pessoa`) REFERENCES `pessoa` (`id_pessoa`),
  CONSTRAINT `equipeevento_ibfk_3` FOREIGN KEY (`id_perfil`) REFERENCES `perfil` (`id_perfil`),
  CONSTRAINT `fk_equipeevento_usuario_alteracao` FOREIGN KEY (`id_usuario_alteracao`) REFERENCES `usuario` (`id_usuario`),
  CONSTRAINT `fk_equipeevento_usuario_criacao` FOREIGN KEY (`id_usuario_criacao`) REFERENCES `usuario` (`id_usuario`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `escala` (
  `id_escala` int NOT NULL AUTO_INCREMENT,
  `id_equipe` int NOT NULL,
  `id_jornada` int NOT NULL,
  `entrada_prevista` time NOT NULL,
  `saida_prevista` time NOT NULL,
  `situacao` varchar(30) NOT NULL DEFAULT 'pendente',
  PRIMARY KEY (`id_escala`),
  UNIQUE KEY `id_equipe` (`id_equipe`,`id_jornada`),
  KEY `id_jornada` (`id_jornada`),
  CONSTRAINT `escala_ibfk_1` FOREIGN KEY (`id_equipe`) REFERENCES `equipeevento` (`id_equipe`),
  CONSTRAINT `escala_ibfk_2` FOREIGN KEY (`id_jornada`) REFERENCES `jornadasetor` (`id_jornada`),
  CONSTRAINT `chk_escala_situacao` CHECK ((lower(`situacao`) in (_cp850'pendente',_cp850'confirmada',_cp850'cancelada')))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `estoque` (
  `id_estoque` int NOT NULL AUTO_INCREMENT,
  `id_local_estoque` int NOT NULL,
  `id_produto` int NOT NULL,
  `quantidade_minima` decimal(10,3) NOT NULL,
  `quantidade_ideal` decimal(10,3) NOT NULL,
  `quantidade_atual` decimal(18,3) NOT NULL DEFAULT '0.000',
  PRIMARY KEY (`id_estoque`),
  UNIQUE KEY `id_local_estoque` (`id_local_estoque`,`id_produto`),
  KEY `id_produto` (`id_produto`),
  CONSTRAINT `estoque_ibfk_1` FOREIGN KEY (`id_local_estoque`) REFERENCES `localestoque` (`id_local_estoque`),
  CONSTRAINT `estoque_ibfk_2` FOREIGN KEY (`id_produto`) REFERENCES `produto` (`id_produto`),
  CONSTRAINT `chk_estoque_quantidades` CHECK (((`quantidade_minima` >= 0) and (`quantidade_ideal` >= `quantidade_minima`) and (`quantidade_atual` >= 0) and (`quantidade_minima` = floor(`quantidade_minima`)) and (`quantidade_ideal` = floor(`quantidade_ideal`)) and (`quantidade_atual` = floor(`quantidade_atual`))))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `estoquesetor` (
  `id_setor` int NOT NULL,
  `id_local_estoque` int NOT NULL,
  `id_evento` int NOT NULL,
  PRIMARY KEY (`id_setor`),
  UNIQUE KEY `uq_estoquesetor_local` (`id_local_estoque`),
  KEY `fk_estoquesetor_setor_evento` (`id_setor`,`id_evento`),
  KEY `fk_estoquesetor_local_evento` (`id_local_estoque`,`id_evento`),
  CONSTRAINT `estoquesetor_ibfk_1` FOREIGN KEY (`id_setor`) REFERENCES `setor` (`id_setor`),
  CONSTRAINT `estoquesetor_ibfk_2` FOREIGN KEY (`id_local_estoque`) REFERENCES `localestoque` (`id_local_estoque`),
  CONSTRAINT `fk_estoquesetor_local_evento` FOREIGN KEY (`id_local_estoque`, `id_evento`) REFERENCES `localestoque` (`id_local_estoque`, `id_evento`),
  CONSTRAINT `fk_estoquesetor_setor_evento` FOREIGN KEY (`id_setor`, `id_evento`) REFERENCES `setor` (`id_setor`, `id_evento`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `evento` (
  `id_evento` int NOT NULL AUTO_INCREMENT,
  `id_instituicao` int NOT NULL,
  `id_tipo_evento` int NOT NULL,
  `nome` varchar(150) NOT NULL,
  `local` varchar(200) NOT NULL,
  `data_inicio` date NOT NULL,
  `data_fim` date NOT NULL,
  `id_responsavel` int NOT NULL,
  `situacao` varchar(30) NOT NULL DEFAULT 'planejado',
  `observacao` text,
  `id_usuario_criacao` int DEFAULT NULL,
  `criado_em` datetime DEFAULT NULL,
  `versao` int unsigned NOT NULL DEFAULT '1',
  PRIMARY KEY (`id_evento`),
  UNIQUE KEY `uq_evento_id_instituicao` (`id_evento`,`id_instituicao`),
  KEY `id_instituicao` (`id_instituicao`),
  KEY `id_tipo_evento` (`id_tipo_evento`),
  KEY `id_responsavel` (`id_responsavel`),
  KEY `ix_evento_usuario_criacao` (`id_usuario_criacao`),
  CONSTRAINT `evento_ibfk_1` FOREIGN KEY (`id_instituicao`) REFERENCES `instituicao` (`id_instituicao`),
  CONSTRAINT `evento_ibfk_2` FOREIGN KEY (`id_tipo_evento`) REFERENCES `tipoevento` (`id_tipo_evento`),
  CONSTRAINT `evento_ibfk_3` FOREIGN KEY (`id_responsavel`) REFERENCES `pessoa` (`id_pessoa`),
  CONSTRAINT `fk_evento_usuario_criacao` FOREIGN KEY (`id_usuario_criacao`) REFERENCES `usuario` (`id_usuario`),
  CONSTRAINT `chk_evento_situacao` CHECK ((lower(`situacao`) in (_cp850'planejado',_cp850'em_andamento',_cp850'finalizado',_cp850'cancelado')))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `fornecedor` (
  `id_fornecedor` int NOT NULL AUTO_INCREMENT,
  `nome` varchar(150) NOT NULL,
  `documento` varchar(18) DEFAULT NULL,
  `telefone` varchar(20) DEFAULT NULL,
  `email` varchar(150) DEFAULT NULL,
  PRIMARY KEY (`id_fornecedor`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `historicoalteracaooperacao` (
  `id_historico` bigint unsigned NOT NULL AUTO_INCREMENT,
  `entidade` varchar(30) NOT NULL,
  `id_entidade` int NOT NULL,
  `id_usuario` int NOT NULL,
  `data_hora` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `justificativa` varchar(500) DEFAULT NULL,
  `dados_anteriores` json NOT NULL,
  `dados_novos` json NOT NULL,
  PRIMARY KEY (`id_historico`),
  KEY `ix_historico_entidade` (`entidade`,`id_entidade`,`data_hora`),
  KEY `ix_historico_usuario` (`id_usuario`,`data_hora`),
  CONSTRAINT `fk_historico_usuario` FOREIGN KEY (`id_usuario`) REFERENCES `usuario` (`id_usuario`),
  CONSTRAINT `chk_historico_entidade` CHECK ((`entidade` in (_utf8mb4'evento',_utf8mb4'equipeevento',_utf8mb4'solicitacao',_utf8mb4'solicitacaoestoquecentral',_utf8mb4'entradamercadoria',_utf8mb4'transferenciaestoque',_utf8mb4'registro')))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `instituicao` (
  `id_instituicao` int NOT NULL AUTO_INCREMENT,
  `nome` varchar(150) NOT NULL,
  `documento` varchar(18) DEFAULT NULL,
  `telefone` varchar(20) DEFAULT NULL,
  `email` varchar(150) DEFAULT NULL,
  PRIMARY KEY (`id_instituicao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `jornadasetor` (
  `id_jornada` int NOT NULL AUTO_INCREMENT,
  `id_setor` int NOT NULL,
  `data` date NOT NULL,
  `id_turno` int NOT NULL,
  PRIMARY KEY (`id_jornada`),
  UNIQUE KEY `id_setor` (`id_setor`,`data`,`id_turno`),
  KEY `id_turno` (`id_turno`),
  CONSTRAINT `jornadasetor_ibfk_1` FOREIGN KEY (`id_setor`) REFERENCES `setor` (`id_setor`),
  CONSTRAINT `jornadasetor_ibfk_2` FOREIGN KEY (`id_turno`) REFERENCES `turno` (`id_turno`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `localestoque` (
  `id_local_estoque` int NOT NULL AUTO_INCREMENT,
  `id_instituicao_central` int DEFAULT NULL,
  `id_evento` int DEFAULT NULL,
  `nome` varchar(100) NOT NULL,
  `ativo` tinyint(1) NOT NULL,
  `eh_geral_evento` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`id_local_estoque`),
  UNIQUE KEY `uq_localestoque_central` (`id_instituicao_central`),
  UNIQUE KEY `uq_localestoque_evento` (`id_local_estoque`,`id_evento`),
  UNIQUE KEY `uq_localestoque_nome_evento` (`id_evento`,`nome`),
  UNIQUE KEY `uq_localestoque_geral_evento` (`id_evento`,(CASE WHEN eh_geral_evento=1 THEN 1 ELSE NULL END)),
  CONSTRAINT `fk_localestoque_evento` FOREIGN KEY (`id_evento`) REFERENCES `evento` (`id_evento`),
  CONSTRAINT `fk_localestoque_central` FOREIGN KEY (`id_instituicao_central`) REFERENCES `instituicao` (`id_instituicao`),
  CONSTRAINT `chk_localestoque_proprietario` CHECK (((id_instituicao_central IS NOT NULL AND id_evento IS NULL AND eh_geral_evento=0) OR (id_instituicao_central IS NULL AND id_evento IS NOT NULL AND eh_geral_evento IN (0,1))))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `perfil` (
  `id_perfil` int NOT NULL AUTO_INCREMENT,
  `nome` varchar(100) NOT NULL,
  PRIMARY KEY (`id_perfil`),
  UNIQUE KEY `uq_perfil_nome` (`nome`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `perfilsistema` (
  `id_perfil_sistema` int NOT NULL AUTO_INCREMENT,
  `nome` varchar(100) NOT NULL,
  PRIMARY KEY (`id_perfil_sistema`),
  UNIQUE KEY `uq_perfilsistema_nome` (`nome`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `pessoa` (
  `id_pessoa` int NOT NULL AUTO_INCREMENT,
  `nome` varchar(100) NOT NULL,
  `telefone` varchar(20) DEFAULT NULL,
  `email` varchar(50) DEFAULT NULL,
  `foto_perfil` mediumblob,
  PRIMARY KEY (`id_pessoa`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `produto` (
  `id_produto` int NOT NULL AUTO_INCREMENT,
  `id_categoria` int NOT NULL,
  `nome` varchar(100) NOT NULL,
  `descricao` text,
  `unidade_medida` varchar(20) NOT NULL DEFAULT 'UN',
  `ativo` tinyint(1) NOT NULL,
  `imagem` mediumblob,
  PRIMARY KEY (`id_produto`),
  KEY `id_categoria` (`id_categoria`),
  CONSTRAINT `produto_ibfk_1` FOREIGN KEY (`id_categoria`) REFERENCES `categoria` (`id_categoria`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `produtofornecedor` (
  `id_produto` int NOT NULL,
  `id_fornecedor` int NOT NULL,
  `codigo_no_fornecedor` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`id_produto`,`id_fornecedor`),
  KEY `id_fornecedor` (`id_fornecedor`),
  CONSTRAINT `produtofornecedor_ibfk_1` FOREIGN KEY (`id_produto`) REFERENCES `produto` (`id_produto`),
  CONSTRAINT `produtofornecedor_ibfk_2` FOREIGN KEY (`id_fornecedor`) REFERENCES `fornecedor` (`id_fornecedor`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `produtosetor` (
  `id_produto_setor` int NOT NULL AUTO_INCREMENT,
  `id_setor` int NOT NULL,
  `id_produto` int NOT NULL,
  `preco_venda` decimal(10,2) NOT NULL,
  PRIMARY KEY (`id_produto_setor`),
  UNIQUE KEY `id_setor` (`id_setor`,`id_produto`),
  KEY `id_produto` (`id_produto`),
  CONSTRAINT `produtosetor_ibfk_1` FOREIGN KEY (`id_setor`) REFERENCES `setor` (`id_setor`),
  CONSTRAINT `produtosetor_ibfk_2` FOREIGN KEY (`id_produto`) REFERENCES `produto` (`id_produto`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `registro` (
  `id_registro` int NOT NULL AUTO_INCREMENT,
  `id_escala` int NOT NULL,
  `id_usuario_responsavel` int NOT NULL,
  `tipo` varchar(20) NOT NULL,
  `data_hora` datetime NOT NULL,
  `observacao` text,
  `chave_operacao` char(36) DEFAULT NULL,
  `criado_em` datetime DEFAULT NULL,
  `id_usuario_alteracao` int DEFAULT NULL,
  `atualizado_em` datetime DEFAULT NULL,
  `versao` int unsigned NOT NULL DEFAULT '1',
  `forma_pagamento` varchar(20) DEFAULT NULL,
  PRIMARY KEY (`id_registro`),
  UNIQUE KEY `uq_registro_chave_operacao` (`chave_operacao`),
  KEY `id_escala` (`id_escala`),
  KEY `id_usuario_responsavel` (`id_usuario_responsavel`),
  KEY `ix_registro_usuario_alteracao` (`id_usuario_alteracao`),
  CONSTRAINT `fk_registro_usuario_alteracao` FOREIGN KEY (`id_usuario_alteracao`) REFERENCES `usuario` (`id_usuario`),
  CONSTRAINT `registro_ibfk_1` FOREIGN KEY (`id_escala`) REFERENCES `escala` (`id_escala`),
  CONSTRAINT `registro_ibfk_2` FOREIGN KEY (`id_usuario_responsavel`) REFERENCES `usuario` (`id_usuario`),
  CONSTRAINT `chk_registro_tipo` CHECK ((lower(`tipo`) in (_utf8mb4'venda',_utf8mb4'perda')))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `registroitem` (
  `id_registro_item` int NOT NULL AUTO_INCREMENT,
  `id_registro` int NOT NULL,
  `id_produto_setor` int NOT NULL,
  `quantidade` decimal(10,3) NOT NULL,
  `preco_unitario_praticado` decimal(10,2) DEFAULT NULL,
  PRIMARY KEY (`id_registro_item`),
  KEY `id_registro` (`id_registro`),
  KEY `id_produto_setor` (`id_produto_setor`),
  CONSTRAINT `registroitem_ibfk_1` FOREIGN KEY (`id_registro`) REFERENCES `registro` (`id_registro`),
  CONSTRAINT `registroitem_ibfk_2` FOREIGN KEY (`id_produto_setor`) REFERENCES `produtosetor` (`id_produto_setor`),
  CONSTRAINT `chk_registroitem_quantidade` CHECK (((`quantidade` > 0) and (`quantidade` = floor(`quantidade`))))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `setor` (
  `id_setor` int NOT NULL AUTO_INCREMENT,
  `id_evento` int NOT NULL,
  `nome` varchar(100) NOT NULL,
  PRIMARY KEY (`id_setor`),
  UNIQUE KEY `id_evento` (`id_evento`,`nome`),
  UNIQUE KEY `uq_setor_id_evento` (`id_setor`,`id_evento`),
  CONSTRAINT `setor_ibfk_1` FOREIGN KEY (`id_evento`) REFERENCES `evento` (`id_evento`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `solicitacao` (
  `id_solicitacao` int NOT NULL AUTO_INCREMENT,
  `id_escala` int NOT NULL,
  `tipo` varchar(30) NOT NULL,
  `descricao` text,
  `data_hora` datetime NOT NULL,
  `situacao` varchar(30) NOT NULL DEFAULT 'pendente',
  `id_usuario_solicitante` int DEFAULT NULL,
  `id_usuario_decisor` int DEFAULT NULL,
  `decidida_em` datetime DEFAULT NULL,
  `criado_em` datetime DEFAULT NULL,
  `versao` int unsigned NOT NULL DEFAULT '1',
  `justificativa_rejeicao` varchar(500) DEFAULT NULL,
  PRIMARY KEY (`id_solicitacao`),
  KEY `id_escala` (`id_escala`),
  KEY `ix_solicitacao_solicitante` (`id_usuario_solicitante`),
  KEY `ix_solicitacao_decisor` (`id_usuario_decisor`),
  CONSTRAINT `fk_solicitacao_decisor` FOREIGN KEY (`id_usuario_decisor`) REFERENCES `usuario` (`id_usuario`),
  CONSTRAINT `fk_solicitacao_solicitante` FOREIGN KEY (`id_usuario_solicitante`) REFERENCES `usuario` (`id_usuario`),
  CONSTRAINT `solicitacao_ibfk_1` FOREIGN KEY (`id_escala`) REFERENCES `escala` (`id_escala`),
  CONSTRAINT `chk_solicitacao_decisao_preenchida` CHECK ((((`id_usuario_decisor` is null) and (`decidida_em` is null)) or ((`id_usuario_decisor` is not null) and (`decidida_em` is not null)))),
  CONSTRAINT `chk_solicitacao_justificativa_rejeicao` CHECK (((`justificativa_rejeicao` is null) or (char_length(trim(`justificativa_rejeicao`)) > 0))),
  CONSTRAINT `chk_solicitacao_situacao` CHECK ((lower(`situacao`) in (_utf8mb4'pendente',_utf8mb4'aprovada',_utf8mb4'atendida',_utf8mb4'rejeitada',_utf8mb4'cancelada')))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `solicitacaoestoquecentral` (
  `id_solicitacao_estoque_central` int NOT NULL AUTO_INCREMENT,
  `id_local_origem` int NOT NULL,
  `id_local_destino` int NOT NULL,
  `id_usuario_solicitante` int NOT NULL,
  `data_hora` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `situacao` varchar(30) NOT NULL DEFAULT 'pendente',
  `id_usuario_decisor` int DEFAULT NULL,
  `decidida_em` datetime DEFAULT NULL,
  `justificativa_rejeicao` varchar(500) DEFAULT NULL,
  `versao` int unsigned NOT NULL DEFAULT '1',
  PRIMARY KEY (`id_solicitacao_estoque_central`),
  KEY `ix_solicitacao_central_pendencia` (`situacao`,`data_hora`),
  KEY `ix_solicitacao_central_solicitante` (`id_usuario_solicitante`,`data_hora`),
  KEY `ix_solicitacao_central_decisor` (`id_usuario_decisor`),
  CONSTRAINT `fk_solicitacao_central_decisor` FOREIGN KEY (`id_usuario_decisor`) REFERENCES `usuario` (`id_usuario`),
  CONSTRAINT `fk_solicitacao_central_solicitante` FOREIGN KEY (`id_usuario_solicitante`) REFERENCES `usuario` (`id_usuario`),
  CONSTRAINT `chk_solicitacao_central_decisao` CHECK ((((`situacao` = _utf8mb4'pendente') and (`id_usuario_decisor` is null) and (`decidida_em` is null)) or ((`situacao` in (_utf8mb4'aprovada',_utf8mb4'rejeitada',_utf8mb4'atendida')) and (`id_usuario_decisor` is not null) and (`decidida_em` is not null)))),
  CONSTRAINT `chk_solicitacao_central_justificativa` CHECK ((((`situacao` = _utf8mb4'rejeitada') and (`justificativa_rejeicao` is not null) and (char_length(trim(`justificativa_rejeicao`)) > 0)) or ((`situacao` <> _utf8mb4'rejeitada') and (`justificativa_rejeicao` is null)))),
  CONSTRAINT `fk_solicitacao_central_origem` FOREIGN KEY (`id_local_origem`) REFERENCES `localestoque` (`id_local_estoque`),
  CONSTRAINT `fk_solicitacao_central_destino` FOREIGN KEY (`id_local_destino`) REFERENCES `localestoque` (`id_local_estoque`),
  CONSTRAINT `chk_solicitacao_central_locais` CHECK ((`id_local_origem` <> `id_local_destino`)),
  CONSTRAINT `chk_solicitacao_central_situacao` CHECK ((`situacao` in (_utf8mb4'pendente',_utf8mb4'aprovada',_utf8mb4'rejeitada',_utf8mb4'atendida')))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `solicitacaoestoquecentralitem` (
  `id_solicitacao_estoque_central_item` int NOT NULL AUTO_INCREMENT,
  `id_solicitacao_estoque_central` int NOT NULL,
  `id_produto` int NOT NULL,
  `quantidade` decimal(10,3) NOT NULL,
  PRIMARY KEY (`id_solicitacao_estoque_central_item`),
  UNIQUE KEY `uq_solicitacao_central_item_produto` (`id_solicitacao_estoque_central`,`id_produto`),
  KEY `ix_solicitacao_central_item_produto` (`id_produto`),
  CONSTRAINT `fk_solicitacao_central_item_cabecalho` FOREIGN KEY (`id_solicitacao_estoque_central`) REFERENCES `solicitacaoestoquecentral` (`id_solicitacao_estoque_central`),
  CONSTRAINT `fk_solicitacao_central_item_produto` FOREIGN KEY (`id_produto`) REFERENCES `produto` (`id_produto`),
  CONSTRAINT `chk_solicitacao_central_item_quantidade` CHECK (((`quantidade` > 0) and (`quantidade` = floor(`quantidade`))))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `solicitacaoitem` (
  `id_solicitacao_item` int NOT NULL AUTO_INCREMENT,
  `id_solicitacao` int NOT NULL,
  `id_produto_setor` int NOT NULL,
  `quantidade` decimal(10,3) NOT NULL,
  PRIMARY KEY (`id_solicitacao_item`),
  KEY `id_solicitacao` (`id_solicitacao`),
  KEY `id_produto_setor` (`id_produto_setor`),
  CONSTRAINT `solicitacaoitem_ibfk_1` FOREIGN KEY (`id_solicitacao`) REFERENCES `solicitacao` (`id_solicitacao`),
  CONSTRAINT `solicitacaoitem_ibfk_2` FOREIGN KEY (`id_produto_setor`) REFERENCES `produtosetor` (`id_produto_setor`),
  CONSTRAINT `chk_solicitacaoitem_quantidade` CHECK (((`quantidade` > 0) and (`quantidade` = floor(`quantidade`))))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `tipoevento` (
  `id_tipo_evento` int NOT NULL AUTO_INCREMENT,
  `nome` varchar(100) NOT NULL,
  PRIMARY KEY (`id_tipo_evento`),
  UNIQUE KEY `nome` (`nome`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `transferenciaestoque` (
  `id_transferencia` int NOT NULL AUTO_INCREMENT,
  `id_local_origem` int NOT NULL,
  `id_local_destino` int NOT NULL,
  `id_solicitacao` int DEFAULT NULL,
  `id_usuario_responsavel` int NOT NULL,
  `data_hora` datetime NOT NULL,
  `observacao` text,
  `chave_operacao` char(36) DEFAULT NULL,
  `criado_em` datetime DEFAULT NULL,
  `id_usuario_alteracao` int DEFAULT NULL,
  `atualizado_em` datetime DEFAULT NULL,
  `versao` int unsigned NOT NULL DEFAULT '1',
  `id_solicitacao_estoque_central` int DEFAULT NULL,
  PRIMARY KEY (`id_transferencia`),
  UNIQUE KEY `uq_transferencia_solicitacao` (`id_solicitacao`),
  UNIQUE KEY `uq_transferencia_chave_operacao` (`chave_operacao`),
  UNIQUE KEY `uq_transferencia_solicitacao_central` (`id_solicitacao_estoque_central`),
  KEY `id_local_origem` (`id_local_origem`),
  KEY `id_local_destino` (`id_local_destino`),
  KEY `id_usuario_responsavel` (`id_usuario_responsavel`),
  KEY `ix_transferencia_usuario_alteracao` (`id_usuario_alteracao`),
  CONSTRAINT `fk_transferencia_solicitacao_central` FOREIGN KEY (`id_solicitacao_estoque_central`) REFERENCES `solicitacaoestoquecentral` (`id_solicitacao_estoque_central`),
  CONSTRAINT `fk_transferencia_usuario_alteracao` FOREIGN KEY (`id_usuario_alteracao`) REFERENCES `usuario` (`id_usuario`),
  CONSTRAINT `transferenciaestoque_ibfk_1` FOREIGN KEY (`id_local_origem`) REFERENCES `localestoque` (`id_local_estoque`),
  CONSTRAINT `transferenciaestoque_ibfk_2` FOREIGN KEY (`id_local_destino`) REFERENCES `localestoque` (`id_local_estoque`),
  CONSTRAINT `transferenciaestoque_ibfk_3` FOREIGN KEY (`id_solicitacao`) REFERENCES `solicitacao` (`id_solicitacao`),
  CONSTRAINT `transferenciaestoque_ibfk_4` FOREIGN KEY (`id_usuario_responsavel`) REFERENCES `usuario` (`id_usuario`),
  CONSTRAINT `transferenciaestoque_chk_1` CHECK ((`id_local_origem` <> `id_local_destino`))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `transferenciaitem` (
  `id_transferencia_item` int NOT NULL AUTO_INCREMENT,
  `id_transferencia` int NOT NULL,
  `id_produto` int NOT NULL,
  `quantidade` decimal(10,3) NOT NULL,
  PRIMARY KEY (`id_transferencia_item`),
  UNIQUE KEY `id_transferencia` (`id_transferencia`,`id_produto`),
  KEY `id_produto` (`id_produto`),
  CONSTRAINT `transferenciaitem_ibfk_1` FOREIGN KEY (`id_transferencia`) REFERENCES `transferenciaestoque` (`id_transferencia`),
  CONSTRAINT `transferenciaitem_ibfk_2` FOREIGN KEY (`id_produto`) REFERENCES `produto` (`id_produto`),
  CONSTRAINT `chk_transferenciaitem_quantidade` CHECK (((`quantidade` > 0) and (`quantidade` = floor(`quantidade`))))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `turno` (
  `id_turno` int NOT NULL AUTO_INCREMENT,
  `nome` varchar(50) NOT NULL,
  PRIMARY KEY (`id_turno`),
  UNIQUE KEY `nome` (`nome`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `usuario` (
  `id_usuario` int NOT NULL AUTO_INCREMENT,
  `id_pessoa` int NOT NULL,
  `id_perfil_sistema` int DEFAULT NULL,
  `login` varchar(50) NOT NULL,
  `senha_hash` varchar(255) NOT NULL,
  `situacao_conta` varchar(30) NOT NULL DEFAULT 'pendente',
  `solicitado_em` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_usuario`),
  UNIQUE KEY `id_pessoa` (`id_pessoa`),
  UNIQUE KEY `login` (`login`),
  KEY `id_perfil_sistema` (`id_perfil_sistema`),
  CONSTRAINT `usuario_ibfk_1` FOREIGN KEY (`id_pessoa`) REFERENCES `pessoa` (`id_pessoa`),
  CONSTRAINT `usuario_ibfk_2` FOREIGN KEY (`id_perfil_sistema`) REFERENCES `perfilsistema` (`id_perfil_sistema`),
  CONSTRAINT `chk_usuario_aprovado_com_perfil` CHECK (((lower(`situacao_conta`) <> _cp850'aprovado') or (`id_perfil_sistema` is not null))),
  CONSTRAINT `chk_usuario_situacao` CHECK ((lower(`situacao_conta`) in (_cp850'pendente',_cp850'aprovado',_cp850'rejeitado')))
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
SET FOREIGN_KEY_CHECKS=1;

-- Totais antigos sem discriminação por modalidade são preservados separadamente.
CREATE TABLE apuracaototallegado (
  id_apuracao int NOT NULL,
  valor_informado decimal(10,2) NOT NULL,
  PRIMARY KEY (id_apuracao),
  CONSTRAINT fk_apuracao_total_legado FOREIGN KEY (id_apuracao) REFERENCES apuracao(id_apuracao)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- As views recompõem os dados de leitura sem armazenar as redundâncias.
CREATE VIEW vw_localestoque AS
SELECT l.id_local_estoque,COALESCE(l.id_instituicao_central,e.id_instituicao) AS id_instituicao,
       l.id_evento,l.nome,l.ativo,(l.id_instituicao_central IS NOT NULL) AS eh_central,l.eh_geral_evento
FROM localestoque l LEFT JOIN evento e ON e.id_evento=l.id_evento;

CREATE VIEW vw_solicitacaoestoquecentral AS
SELECT s.id_solicitacao_estoque_central,
       CASE WHEN o.eh_central=1 THEN 'liberacao' ELSE 'devolucao' END AS tipo,
       o.id_instituicao,COALESCE(o.id_evento,d.id_evento) AS id_evento,
       s.id_local_origem,s.id_local_destino,s.id_usuario_solicitante,s.data_hora,
       s.situacao,s.id_usuario_decisor,s.decidida_em,s.justificativa_rejeicao,s.versao
FROM solicitacaoestoquecentral s
JOIN vw_localestoque o ON o.id_local_estoque=s.id_local_origem
JOIN vw_localestoque d ON d.id_local_estoque=s.id_local_destino;

CREATE VIEW vw_apuracao AS
SELECT a.id_apuracao,a.id_jornada,
       COALESCE(h.valor_informado,a.valor_dinheiro+a.valor_pix+a.valor_cartao) AS valor_informado,
       a.data_hora,a.id_usuario_responsavel,a.observacao,a.valor_dinheiro,a.valor_pix,a.valor_cartao
FROM apuracao a LEFT JOIN apuracaototallegado h ON h.id_apuracao=a.id_apuracao;

DELIMITER $$
CREATE PROCEDURE validar_rota_central(IN origem int, IN destino int)
BEGIN
  DECLARE instituicao_origem int;
  DECLARE instituicao_destino int;
  DECLARE central_origem tinyint;
  DECLARE central_destino tinyint;
  DECLARE geral_origem tinyint;
  DECLARE geral_destino tinyint;
  SELECT id_instituicao,eh_central,eh_geral_evento
    INTO instituicao_origem,central_origem,geral_origem
    FROM vw_localestoque WHERE id_local_estoque=origem;
  SELECT id_instituicao,eh_central,eh_geral_evento
    INTO instituicao_destino,central_destino,geral_destino
    FROM vw_localestoque WHERE id_local_estoque=destino;
  IF instituicao_origem IS NULL OR instituicao_destino IS NULL
     OR instituicao_origem<>instituicao_destino
     OR NOT ((central_origem=1 AND geral_destino=1) OR (geral_origem=1 AND central_destino=1)) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Origem e destino incompatíveis com a solicitação central.';
  END IF;
END$$
CREATE TRIGGER solicitacao_central_rota_insert BEFORE INSERT ON solicitacaoestoquecentral FOR EACH ROW
BEGIN
  CALL validar_rota_central(NEW.id_local_origem,NEW.id_local_destino);
END$$
CREATE TRIGGER solicitacao_central_rota_update BEFORE UPDATE ON solicitacaoestoquecentral FOR EACH ROW
BEGIN
  IF NEW.id_local_origem<>OLD.id_local_origem OR NEW.id_local_destino<>OLD.id_local_destino THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Os locais de uma solicitação existente não podem ser substituídos.';
  END IF;
  CALL validar_rota_central(NEW.id_local_origem,NEW.id_local_destino);
END$$
CREATE TRIGGER local_preservar_rota BEFORE UPDATE ON localestoque FOR EACH ROW
BEGIN
  IF (NOT (NEW.id_instituicao_central <=> OLD.id_instituicao_central)
      OR NOT (NEW.id_evento <=> OLD.id_evento) OR NEW.eh_geral_evento<>OLD.eh_geral_evento) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='O proprietário e a classificação do local são permanentes.';
  END IF;
END$$
CREATE TRIGGER evento_preservar_instituicao BEFORE UPDATE ON evento FOR EACH ROW
BEGIN
  IF NEW.id_instituicao<>OLD.id_instituicao THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='A instituição do evento é permanente.';
  END IF;
END$$
CREATE TRIGGER apuracao_validar_modalidades BEFORE INSERT ON apuracao FOR EACH ROW
BEGIN
  IF NEW.valor_dinheiro IS NULL OR NEW.valor_pix IS NULL OR NEW.valor_cartao IS NULL
     OR NEW.valor_dinheiro<0 OR NEW.valor_pix<0 OR NEW.valor_cartao<0
     OR NEW.valor_dinheiro+NEW.valor_pix+NEW.valor_cartao>99999999.99 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Novas conferências exigem valores válidos nas três modalidades.';
  END IF;
END$$
DELIMITER ;

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

-- Consultas das telas do CASE.
-- As variáveis abaixo representam os filtros usados nas telas.
-- Senhas e imagens não são exibidas nas consultas.
SET SESSION transaction_read_only = IF(COALESCE(@case_instalacao_bloqueada, 0)=1,
    1, @@session.transaction_read_only);
USE case_commercial;

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
SET @jornada = (
    SELECT MIN(id_jornada)
    FROM jornadasetor);

SET @setor = (
    SELECT id_setor
    FROM jornadasetor
    WHERE id_jornada = @jornada);

SET @data = (
    SELECT `data`
    FROM jornadasetor
    WHERE id_jornada = @jornada);

SET @turno = (
    SELECT id_turno
    FROM jornadasetor
    WHERE id_jornada = @jornada);

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
JOIN vw_localestoque l ON l.id_local_estoque = es.id_local_estoque
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
    FROM vw_localestoque);

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
    FROM vw_localestoque);

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
FROM vw_localestoque
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
FROM vw_localestoque
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
FROM vw_localestoque
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
FROM vw_localestoque
WHERE id_evento = @evento
    AND eh_geral_evento = 1
ORDER BY id_local_estoque;

-- 57. LocalEstoqueRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_local_estoque)
    FROM vw_localestoque);

SELECT id_local_estoque,
    id_instituicao,
    id_evento,
    nome,
    ativo,
    eh_central,
    eh_geral_evento
FROM vw_localestoque
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
    SELECT id_local_destino
    FROM entradamercadoria
    ORDER BY id_entrada
    LIMIT 1);

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
FROM vw_solicitacaoestoquecentral
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
FROM vw_solicitacaoestoquecentral s
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
FROM vw_solicitacaoestoquecentral
WHERE id_usuario_solicitante = @usuario
ORDER BY data_hora DESC, id_solicitacao_estoque_central DESC;

-- 64. SolicitacaoEstoqueCentralRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_solicitacao_estoque_central)
    FROM vw_solicitacaoestoquecentral);

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
FROM vw_solicitacaoestoquecentral
WHERE id_solicitacao_estoque_central = @id;

-- 65. SolicitacaoEstoqueCentralRepository.ListarItens
SET @id = (
    SELECT MIN(id_solicitacao_estoque_central)
    FROM vw_solicitacaoestoquecentral);

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
    FROM vw_solicitacaoestoquecentral);

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
    JOIN vw_localestoque l ON l.id_local_estoque = en.id_local_destino
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
    JOIN vw_localestoque o ON o.id_local_estoque = tr.id_local_origem
    JOIN vw_localestoque d ON d.id_local_estoque = tr.id_local_destino
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
    SELECT MIN(id_registro)
    FROM registro);

SET @perfil = 'registro';

-- Sem correções nessa operação, o histórico fica vazio.

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
    SELECT id_local_origem
    FROM transferenciaestoque
    ORDER BY id_transferencia
    LIMIT 1);

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
FROM vw_apuracao
WHERE id_jornada = @id
ORDER BY id_apuracao;

-- 89. ApuracaoRepository.BuscarPorId
SET @id = (
    SELECT MIN(id_apuracao)
    FROM vw_apuracao);

SELECT id_apuracao,
    id_jornada,
    valor_informado,
    data_hora,
    id_usuario_responsavel,
    observacao,
    valor_dinheiro,
    valor_pix,
    valor_cartao
FROM vw_apuracao
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
FROM vw_solicitacaoestoquecentral so
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
FROM vw_apuracao a
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
