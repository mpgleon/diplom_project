﻿namespace diplom_project.Models;
public class Listing
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int HouseTypeId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal? PerHour { get; set; }
    public decimal? PerDay { get; set; }
    public decimal? PerMonth { get; set; }
    public string Location { get; set; }
    public DateTime CreatedDate { get; set; }
    public User User { get; set; }
    public string? Model3DUrl { get; set; } // Путь к 3D-модели
    public bool IsModerated { get; set; } // Статус модерации
    public HouseType HouseType { get; set; }
    public ICollection<ListingAmenity> ListingAmenities { get; set; } = new List<ListingAmenity>();
    public ICollection<ListingMainFeature> ListingMainFeatures { get; set; } = new List<ListingMainFeature>(); // Новая связь
    public ICollection<ListingPhoto> ListingPhotos { get; set; } = new List<ListingPhoto>(); // Инициализация
}