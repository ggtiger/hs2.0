-- ============================================================
-- AI 开发助理 — 物理表创建脚本
-- 模块: RS_MAIDEV / RS_MAIDEVUPG
-- 说明: 6 张物理表，字段名大写无下划线全连写
-- ============================================================

-- ============================================================
-- 1. tss_aidev_session — 开发会话
-- ============================================================
CREATE TABLE IF NOT EXISTS tss_aidev_session (
  ID            VARCHAR(36)  NOT NULL COMMENT '主键',
  SESSIONCODE   VARCHAR(50)  NOT NULL COMMENT '会话编码',
  SESSIONNAME   VARCHAR(200) NOT NULL COMMENT '会话名称',
  SESSIONTYPE   VARCHAR(16)  NULL COMMENT '会话类型 NEW/MODIFY',
  TARGETMODULE  VARCHAR(64)  NULL COMMENT '目标模块编码',
  INTENT        TEXT         NULL COMMENT '开发意图描述',
  STATUS        VARCHAR(16)  NOT NULL DEFAULT 'DRAFT' COMMENT '状态 DRAFT/GENERATING/REVIEWING/EXPORTED/ARCHIVED',
  CREATEDBY     VARCHAR(36)  NULL COMMENT '创建人ID',
  CREATEDTIME   DATETIME     NULL COMMENT '创建时间',
  CLOSEDATE     DATETIME     NULL COMMENT '关闭日期',
  CHANGESETID   VARCHAR(36)  NULL COMMENT '关联变更包ID',
  REMARK        VARCHAR(500) NULL COMMENT '备注',
  ISDELETED     TINYINT      NOT NULL DEFAULT 0 COMMENT '逻辑删除 0未删除 1已删除',
  PRIMARY KEY (ID),
  UNIQUE KEY uk_aisess_code (SESSIONCODE),
  KEY idx_aisess_code (SESSIONCODE),
  KEY idx_aisess_changeset (CHANGESETID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='AI开发会话';

-- ============================================================
-- 2. tss_aidev_changeset — 变更包
-- ============================================================
CREATE TABLE IF NOT EXISTS tss_aidev_changeset (
  ID               VARCHAR(36)  NOT NULL COMMENT '主键',
  SESSIONID        VARCHAR(36)  NOT NULL COMMENT '会话ID',
  CHANGESETCODE    VARCHAR(50)  NOT NULL COMMENT '变更包编码',
  TITLE            VARCHAR(200) NOT NULL COMMENT '标题',
  SOURCE           VARCHAR(16)  NULL COMMENT '来源 NEW/MODIFY',
  INTENT           TEXT         NULL COMMENT '意图描述',
  VALIDATIONPASSED TINYINT      NOT NULL DEFAULT 0 COMMENT '校验是否通过 0否 1是',
  VALIDATIONREPORT TEXT         NULL COMMENT '校验报告',
  ITEMCOUNT        INT          NOT NULL DEFAULT 0 COMMENT '变更项数量',
  CREATEDTIME      DATETIME     NULL COMMENT '创建时间',
  ISDELETED        TINYINT      NOT NULL DEFAULT 0 COMMENT '逻辑删除 0未删除 1已删除',
  PRIMARY KEY (ID),
  UNIQUE KEY uk_aics_code (CHANGESETCODE),
  KEY idx_aics_session (SESSIONID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='AI开发变更包';

-- ============================================================
-- 3. tss_aidev_changeitem — 变更项
-- ============================================================
CREATE TABLE IF NOT EXISTS tss_aidev_changeitem (
  ID             VARCHAR(36)  NOT NULL COMMENT '主键',
  CHANGESETID    VARCHAR(36)  NOT NULL COMMENT '变更包ID',
  ITEMSEQ        INT          NULL COMMENT '项序号',
  CATEGORY       VARCHAR(32)  NULL COMMENT '类别 physical_table/dataview/field/ui/dict/filter/module/api/menu/permission/billflow',
  ACTION         VARCHAR(16)  NULL COMMENT '操作 create/alter/update/delete',
  TOOL           VARCHAR(64)  NULL COMMENT '生成工具',
  TARGET         VARCHAR(128) NULL COMMENT '目标对象',
  SQLCONTENT     LONGTEXT     NULL COMMENT 'SQL内容',
  METADATA       TEXT         NULL COMMENT '元数据JSON',
  RATIONALE      TEXT         NULL COMMENT '设计理由',
  WARNINGS       TEXT         NULL COMMENT '警告信息',
  DEPENDSON      VARCHAR(500) NULL COMMENT '依赖项ID列表逗号分隔',
  ITEMSTATUS     VARCHAR(16)  NOT NULL DEFAULT 'DRAFT' COMMENT '项状态 DRAFT/CONFIRMED/REJECTED',
  CONFIRMEDBY    VARCHAR(36)  NULL COMMENT '确认人ID',
  CONFIRMEDTIME  DATETIME     NULL COMMENT '确认时间',
  CONFIRMORDER   INT          NULL COMMENT '确认顺序',
  ISDELETED      TINYINT      NOT NULL DEFAULT 0 COMMENT '逻辑删除 0未删除 1已删除',
  PRIMARY KEY (ID),
  KEY idx_aici_changeset (CHANGESETID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='AI开发变更项';

-- ============================================================
-- 4. tss_aidev_upgrade — 升级记录
-- ============================================================
CREATE TABLE IF NOT EXISTS tss_aidev_upgrade (
  ID            VARCHAR(36)  NOT NULL COMMENT '主键',
  UPGRADECODE   VARCHAR(50)  NOT NULL COMMENT '升级编码',
  SESSIONCODE   VARCHAR(50)  NULL COMMENT '会话编码',
  SESSIONNAME   VARCHAR(200) NULL COMMENT '会话名称',
  SESSIONTYPE   VARCHAR(16)  NULL COMMENT '会话类型',
  TARGETMODULE  VARCHAR(64)  NULL COMMENT '目标模块编码',
  INTENT        TEXT         NULL COMMENT '意图描述',
  SCRIPTCONTENT LONGTEXT     NULL COMMENT '脚本内容',
  SCRIPTHASH    VARCHAR(64)  NULL COMMENT '脚本哈希',
  ITEMCOUNT     INT          NOT NULL DEFAULT 0 COMMENT '变更项数量',
  STATUS        VARCHAR(16)  NOT NULL DEFAULT 'PENDING' COMMENT '状态 PENDING/RUNNING/SUCCESS/FAILED/ROLLEDBACK',
  EXECUTEDBY    VARCHAR(36)  NULL COMMENT '执行人ID',
  EXECUTEDTIME  DATETIME     NULL COMMENT '执行时间',
  DURATIONMS    INT          NULL COMMENT '执行时长毫秒',
  ERRORMSG      TEXT         NULL COMMENT '错误信息',
  ROLLBACKSCRIPT LONGTEXT    NULL COMMENT '回滚脚本',
  ISDELETED     TINYINT      NOT NULL DEFAULT 0 COMMENT '逻辑删除 0未删除 1已删除',
  PRIMARY KEY (ID),
  UNIQUE KEY uk_aiupg_code (UPGRADECODE),
  KEY idx_aiupg_code (UPGRADECODE),
  KEY idx_aiupg_sessioncode (SESSIONCODE)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='AI开发升级记录';

-- ============================================================
-- 5. tss_aidev_upgrade_log — 升级日志
-- ============================================================
CREATE TABLE IF NOT EXISTS tss_aidev_upgrade_log (
  ID            VARCHAR(36)  NOT NULL COMMENT '主键',
  UPGRADEID     VARCHAR(36)  NOT NULL COMMENT '升级记录ID',
  ITEMID        VARCHAR(36)  NULL COMMENT '变更项ID',
  ITEMCATEGORY  VARCHAR(32)  NULL COMMENT '项类别',
  ITEMACTION    VARCHAR(16)  NULL COMMENT '项操作',
  ITEMTARGET    VARCHAR(128) NULL COMMENT '项目标',
  SQLSNIPPET    TEXT         NULL COMMENT 'SQL片段',
  STATUS        VARCHAR(16)  NULL COMMENT '状态',
  ERRORMSG      TEXT         NULL COMMENT '错误信息',
  ROWSAFFECTED  INT          NULL COMMENT '影响行数',
  EXECUTEDTIME  DATETIME     NULL COMMENT '执行时间',
  PRIMARY KEY (ID),
  KEY idx_aiupgl_upgrade (UPGRADEID),
  KEY idx_aiupgl_item (ITEMID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='AI开发升级日志';

-- ============================================================
-- 6. tss_aidev_upgrade_snapshot — 升级快照（支持回滚）
-- ============================================================
CREATE TABLE IF NOT EXISTS tss_aidev_upgrade_snapshot (
  ID             VARCHAR(36)  NOT NULL COMMENT '主键',
  UPGRADEID      VARCHAR(36)  NOT NULL COMMENT '升级记录ID',
  OBJECTTYPE     VARCHAR(32)  NULL COMMENT '对象类型 TABLE/RESOURCE/RESFIELD/FUNC',
  OBJECTNAME     VARCHAR(128) NULL COMMENT '对象名称',
  SNAPSHOTBEFORE LONGTEXT     NULL COMMENT '变更前快照JSON',
  SNAPSHOTAFTER  LONGTEXT     NULL COMMENT '变更后快照JSON',
  PRIMARY KEY (ID),
  KEY idx_aiupgs_upgrade (UPGRADEID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='AI开发升级快照';
