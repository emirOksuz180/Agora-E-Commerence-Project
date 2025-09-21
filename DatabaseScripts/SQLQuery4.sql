INSERT INTO Sliders (SliderTitle, SliderDescription, ImageUrl, IsActive, DisplayOrder)
VALUES 
('Slider-1-baslik', 'Slider-1-aciklama', 'slider-1.jpeg', 1, 1),
('Slider-2-baslik', 'Slider-2-aciklama', 'slider-2.jpeg', 1, 2),
('Slider-3-baslik', 'Slider-3-aciklama', 'slider-3.jpeg', 1, 3);


DELETE FROM Sliders;


DBCC CHECKIDENT ('Sliders', RESEED, 0);