/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

global using System;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.Diagnostics;
global using System.Text.Json;
global using Mapster;
global using Furion;
global using Furion.DependencyInjection;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Authorization;
global using Microsoft.AspNetCore.Mvc.Controllers;
global using Microsoft.AspNetCore.Mvc.Filters;
global using Domain.Entities;
global using Application.DTOs;
global using Application.Services;
global using Infrastructure.Persistence;
global using UI.Filters;
