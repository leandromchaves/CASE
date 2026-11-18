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
