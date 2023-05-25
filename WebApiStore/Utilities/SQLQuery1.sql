SELECT * FROM products;

SELECT TOP(1) Category, UserId, SUM(Quantity) AS "Quantity"
  FROM Products AS p
	   INNER JOIN OrdersProducts AS op ON p.Id = op.ProductId
	   INNER JOIN Orders AS o ON op.OrderId = o.Id
 WHERE UserId = '45ab711a-971e-476d-a73e-4b58ba3865bb'
 GROUP BY Category, UserId
 ORDER BY Quantity DESC