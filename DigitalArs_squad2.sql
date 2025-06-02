--REQUERIMIENTOS DE LA ETAPA:
--Modelado de la Base de Datos de las entidades necesarias para la gestión de la billetera virtual
--Implementación de operaciones CRUD

--DML
--Creación base de datos

CREATE DATABASE DigitalArs;

use DigitalArs;

--Creación tablas
CREATE TABLE Usuario (
    nro_cliente INT PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL,
    apellido VARCHAR(50) NOT NULL,
    direccion VARCHAR(100),
    mail VARCHAR(100) UNIQUE NOT NULL,
    estado BIT NOT NULL,  -- 1 = activo, 0 = inactivo
	tipo_cliente VARCHAR (20) NOT NULL, --ADMINISTRADOR / BILLETERA
    telefono INT
);


CREATE TABLE Cuenta (
    nro_cuenta INT PRIMARY KEY,
    producto VARCHAR(50) NOT NULL,
    CBU CHAR(22) NOT NULL UNIQUE, -- CBU en Argentina tiene 22 dígitos
    estado BIT NOT NULL,          -- 1 = activa, 0 = inactiva
    nro_cliente INT NOT NULL,
    rol_cta VARCHAR(20),          -- por ejemplo: 'titular', 'autorizado'
	
    FOREIGN KEY (nro_cliente) REFERENCES Usuario(nro_cliente)
);


 CREATE TABLE Transaccion (
    codigo_transaccion INT PRIMARY KEY,
    descripcion VARCHAR(100) NOT NULL
);

CREATE TABLE Movimiento (
    id_trx INT PRIMARY KEY,
    fecha DATETIME NOT NULL,
    monto DECIMAL(12,2) NOT NULL,
    nro_cuenta_orig INT NOT NULL,--cta donde impacta mov
	nro_cuenta_dest INT,--cta para info
    codigo_transaccion INT,
    FOREIGN KEY (nro_cuenta_orig) REFERENCES Cuenta(nro_cuenta),
    FOREIGN KEY (codigo_transaccion) REFERENCES Transaccion(codigo_transaccion)
 
);


CREATE TABLE Permisos (
    nro_usuario INT,
    acceso VARCHAR(20) NOT NULL,

    PRIMARY KEY (nro_usuario, acceso),
    FOREIGN KEY (nro_usuario) REFERENCES Usuario(nro_cliente)
);


CREATE TABLE Plazo_fijo (
    id_pf INT PRIMARY KEY,                     -- ID del plazo fijo
    nro_cuenta INT NOT NULL,                   -- FK hacia Cuenta
    monto DECIMAL(12,2) NOT NULL,              -- Monto invertido
    plazo INT NOT NULL,                        -- Plazo en días
    tasa_interes DECIMAL(5,2) NOT NULL,        -- Tasa en porcentaje (%)

    FOREIGN KEY (nro_cuenta) REFERENCES Cuenta(nro_cuenta)
);

--DDL

INSERT INTO Usuario (nro_cliente, nombre, apellido, direccion, mail, estado, tipo_cliente, telefono)
VALUES 
  (1001, 'Juan', 'Pérez', 'Av. Belgrano 123', 'juan.perez@email.com', 1, 'Administrador','542901233445'),
  (1002, 'Lucía', 'Gómez', 'Calle Falsa 742', 'lucia.gomez@email.com', 1,'Billetera', '542964413284'),
  (1003, 'Mario', 'Rodríguez', 'Yareta 900', 'mario.rod@email.com', 0, 'Billetera','54290619116'),
  (1004, 'Ana', 'López', 'Hol Hol 455', 'ana.lopez@email.com', 1, 'Billetera','542964452178'),
  (1005, 'Kaine', 'Beban', 'Av. Kami 300', 'kbeban@email.com', 'Billetera',1, '542901607146'),
  (1006, 'Carlos', 'Díaz', 'Kupanaka 300', 'carlos.diaz@email.com', 'Billetera',1, '542964617525'),
  (1007, 'Natalia', 'Soto', 'Av. Peru 3597', 'nsoto@email.com', 'Billetera',1, '542901428164'),
  (1008, 'Marcelo', 'Vargas', 'Kuanip 945', 'mvargas@email.com', 'Billetera',1, '542964606042');



  INSERT INTO Cuenta (nro_cuenta, producto, CBU, estado, nro_cliente, rol_cta)
VALUES (2001, 'Caja de Ahorro', '2680000611080202017842', 1, 1005, 'Titular'),
(2002, 'Cuenta Corriente', '2680000611080202017828', 1, 1001, 'Titular'),
(2003, 'Caja de Ahorro',    '2680000611080202017829', 1, 1002, 'Titular'),
(2004, 'Cuenta Corriente', '2680000611080202017830', 1, 1003, 'Autorizado'),
(2005, 'Caja de Ahorro',    '2680000611080202017831', 0, 1004, 'Titular'),
(2006, 'Cuenta Corriente', '2680000611080202017832', 1, 1005, 'Titular'),
(2007, 'Caja de Ahorro', '2680000611080207017842', 1, 1005, 'Titular');


INSERT INTO Transaccion (codigo_transaccion, descripcion)
VALUES 
(1, 'Credito por transferencia'), --SUMA/CRÉDITO
(2, 'Transferencia a otra cuenta'), --RESTA/DÉBITO
(3, 'Deposito cuenta propia'), --SUMA
(4, 'Compra en comercio'), --RESTA
(5, 'Recarga de saldo virtual'); --RESTA

INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (1, GETDATE(), 15000.00,2001,2002,1);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (2, GETDATE(), 17710.71,2003,null,5);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (3, GETDATE(), 13038.18,2004,2001,1);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (4, GETDATE(), 13068.37,2001,null,4);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (5, GETDATE(), 16853.08,2002,2001,2);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (6, GETDATE(), 18159.73,2001,2004,2);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (7, GETDATE(), 18146.88,2004,null,3);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (8, GETDATE(), 12358.07,2002,null,3);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (9, GETDATE(), 10360.72,2005,null,3);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (10, GETDATE(), 19186.38,2003,null,4);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (11, GETDATE(), 19004.2,2005,null,4);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (12, GETDATE(), 14315.61,2001,2003,2);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (13, GETDATE(), 18849.38,2005,null,3);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (14, GETDATE(), 12236.84,2001,null,5);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (15, GETDATE(), 12697.14,2005,null,3);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (16, GETDATE(), 16408.68,2003,2001,1);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (17, GETDATE(), 12243.55,2002,2001,2);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (18, GETDATE(), 19284.4,2001,null,3);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (19, GETDATE(), 16465.36,2003,null,4);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (20, GETDATE(), 10761.99,2001,2005,2);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (21, GETDATE(), 18156.23,2005,null,5);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (22, GETDATE(), 17693.01,2001,2002,2);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (23, GETDATE(), 12760.84,2004,null,5);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (24, GETDATE(), 18355.88,2001,2004,2);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest, codigo_transaccion) VALUES (25, GETDATE(), 10704.52,2005,null,1);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest,codigo_transaccion) VALUES (26, GETDATE(),2458421.72,2005,null,3);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest,codigo_transaccion) VALUES (27, GETDATE(),44234.45,2001,null,3);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest,codigo_transaccion) VALUES (28, GETDATE(),215487.78,2004,null,3);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest,codigo_transaccion) VALUES (29, GETDATE(),4421548.64,2002,null,1);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest,codigo_transaccion) VALUES (30, GETDATE(),45462.78,2005,null,2);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest,codigo_transaccion) VALUES (31, GETDATE(),4564544.94,2003,null,2);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest,codigo_transaccion) VALUES (32, GETDATE(),1259.47,2004,null,2);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest,codigo_transaccion) VALUES (33, GETDATE(),3799182.46,2005,2003,2);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest,codigo_transaccion) VALUES (34, GETDATE(),1375415.84,2004,2001,1);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest,codigo_transaccion) VALUES (35, GETDATE(),25476.45,2003,2005,3);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest,codigo_transaccion) VALUES (36, GETDATE(),1857432.58,2001,2002,1);
INSERT INTO Movimiento (id_trx, fecha, monto, nro_cuenta_orig, nro_cuenta_dest,codigo_transaccion) VALUES (37, GETDATE(),54590.52,2002,2001,2);


INSERT INTO Permisos (nro_usuario, acceso)
VALUES 
(1001, 'admin'),
(1002, 'usuario'),
(1003, 'usuario'),
(1004, 'usuario'),
(1005, 'usuario');



INSERT INTO Plazo_fijo (id_pf, nro_cuenta, monto, plazo, tasa_interes)
VALUES 
(1, 2001, 100000.00, 90, 5.50),
(2, 2002, 50000.00, 120, 6.75),
(3, 2003, 75000.00, 180, 7.00),
(4, 2004, 30000.00, 60, 4.80),
(5, 2005, 150000.00, 365, 8.25),
(6, 2001, 60000.00, 30, 4.00);



--Optimizacion de consultas JOINs, indices y querys anidadas en FROM y WHERE

--clientes con mas de una cuenta
SELECT 
    nro_cliente,
    COUNT(*) AS cantidad_cuentas
FROM Cuenta
GROUP BY nro_cliente
HAVING COUNT(*) > 1;

--más de un movimiento por cuenta

SELECT 
    nro_cuenta_orig,
    COUNT(*) AS cantidad_movimientos
FROM Movimiento
GROUP BY nro_cuenta_orig
HAVING COUNT(*) > 1;

--Consultas con JOIN
-- cantidad de transacciones de 'deposito a otra cuenta' por cliente 
SELECT 
    u.nro_cliente,
    u.nombre,
    u.apellido,
    COUNT(m.id_trx) AS cantidad_depositos
FROM Usuario u
LEFT OUTER JOIN Cuenta c
ON c.nro_cliente = u.nro_cliente
LEFT OUTER JOIN Movimiento m 
ON m.nro_cuenta_orig = c.nro_cuenta
GROUP BY u.nro_cliente, u.nombre, u.apellido;

--Cantidad de transacciones por tipo 
SELECT 
    t.descripcion AS tipo_transaccion,
    COUNT(*) AS cantidad_total
FROM Movimiento m
JOIN Transaccion t ON m.codigo_transaccion = t.codigo_transaccion
GROUP BY t.descripcion
ORDER BY cantidad_total DESC;

--QUERYS ANIDADOS

--Detalle de movimientos por usuario con descripción de transacción

SELECT 
    u.nro_cliente,
    u.nombre,
    u.apellido,
    c.nro_cuenta,
    m.id_trx,
    m.fecha,
    m.monto,
    (
        SELECT t.descripcion
        FROM Transaccion t
        WHERE t.codigo_transaccion = m.codigo_transaccion
    ) AS tipo_transaccion
FROM Usuario u
JOIN Cuenta c ON u.nro_cliente = c.nro_cliente
JOIN Movimiento m ON c.nro_cuenta = m.nro_cuenta_orig;



-- Saldo por cuenta

SELECT 
    cuenta.nro_cuenta,
    ISNULL(ingresos.ingresos, 0) - ISNULL(egresos.egresos, 0) AS saldo_final
FROM (
    SELECT DISTINCT nro_cuenta
    FROM (
        SELECT nro_cuenta_orig AS nro_cuenta FROM Movimiento
         ) AS cuenta
) AS cuenta
OUTER APPLY (
    -- Suma de ingresos por cuenta: depósitos propios y créditos por transferencia recibida
    SELECT SUM(monto) AS ingresos
    FROM Movimiento m
    WHERE 
        (codigo_transaccion in(1, 3) AND m.nro_cuenta_orig = cuenta.nro_cuenta) 
       ) AS ingresos
OUTER APPLY (
    -- Suma de egresos por cuenta: transferencias, compras, recargas
    SELECT SUM(monto) AS egresos
    FROM Movimiento
    WHERE 
        codigo_transaccion IN (2, 4, 5)
        AND nro_cuenta_orig = cuenta.nro_cuenta
) AS egresos;
