using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using astroEMS.Models;
using astroEMS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace astroEMS.Controllers
{
    [Authorize]
    public class EmployeesController : Controller
  {
    private readonly AppDbContext _context;

    public EmployeesController(AppDbContext context)
    {
        _context = context;
    }

        // GET: EMPLOYEES
        public async Task<IActionResult> Index(string searchString)
        {
            var employees = from e in _context.Employees select e;

            if (!string.IsNullOrEmpty(searchString))
            {
                employees = employees.Where(e =>
                    e.FirstName.Contains(searchString) ||
                    e.LastName.Contains(searchString) ||
                    e.EmployeeNumber.Contains(searchString) ||
                    e.Department.Contains(searchString) ||
                    e.Position.Contains(searchString));
            }

            ViewData["CurrentFilter"] = searchString;

            return View(await employees.ToListAsync());
        }

        // GET: EMPLOYEES/Details/5
        public async Task<IActionResult> Details(int? employeeid)
    {
        if (employeeid == null)
        {
            return NotFound();
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(m => m.EmployeeID == employeeid);
        if (employee == null)
        {
            return NotFound();
        }

        return View(employee);
    }
        private async Task<string> GenerateNextEmployeeNumber()
        {
            const string prefix = "ASMI-";

            var lastEmployee = await _context.Employees
                .OrderByDescending(e => e.EmployeeID)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastEmployee != null && lastEmployee.EmployeeNumber.StartsWith(prefix))
            {
                string  numberPart = lastEmployee.EmployeeNumber.Substring(prefix.Length);
                if(int.TryParse(numberPart,out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }
            return $"{prefix}{nextNumber:D4}";
        }

        // GET: EMPLOYEES/Create
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create()
    {
            ViewBag.NextEmployeeNumber = await GenerateNextEmployeeNumber ();
            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "Name", "Name");
            ViewBag.Positions = new SelectList(await _context.Positions.OrderBy(p => p.Name).ToListAsync(), "Name", "Name");
        return View();
    }

        // POST: EMPLOYEES/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,MiddleName,Gender,DateOfBirth,ContactNumber,Email,Address,Department,Position,DateHired,EmploymentStatus,EmploymentType,BasicSalary,SSSNumber,PhilHealthNumber,PagIBIGNumber,TINNumber,WorkSchedule")] Employee employee)
        {
            // Assign BEFORE validation check 
            employee.EmployeeNumber = await GenerateNextEmployeeNumber();
            employee.DateCreated = DateTime.Now;

            ModelState.Remove("EmployeeNumber");
            ModelState.Remove("DateCreated");
            ModelState.Remove("DateUpdated");
            ModelState.Remove("EmployeeID");

            if (ModelState.IsValid)
            {
                _context.Add(employee);
                await _context.SaveChangesAsync();

                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Models.User>();
                var user = new Models.User
                {
                    EmployeeID = employee.EmployeeID,
                    Username = employee.EmployeeNumber,
                    Role = "Employee",
                    IsActive = true
                };
                user.PasswordHash = hasher.HashPassword(user, employee.EmployeeNumber);

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Employee created. Login: Username = {employee.EmployeeNumber}, Password = {employee.EmployeeNumber}";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.NextEmployeeNumber = employee.EmployeeNumber;
            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "Name", "Name", employee.Department);
            ViewBag.Position = new SelectList(await _context.Positions.OrderBy(d => d.Name).ToListAsync(), "Name", "Name", employee.Position);
            return View(employee);
        }
        // GET: EMPLOYEES/Edit/5
        public async Task<IActionResult> Edit(int? employeeid)
        {
            
        if (employeeid == null)
        {
            return NotFound();
        }

        var employee = await _context.Employees.FindAsync(employeeid);
        if (employee == null)
        {
            return NotFound();
        }
            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "Name", "Name", employee.Department);
            ViewBag.Position = new SelectList(await _context.Positions.OrderBy(d => d.Name).ToListAsync(), "Name", "Name", employee.Position);
            return View(employee);
        }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? employeeid, [Bind("EmployeeID,EmployeeNumber,FirstName,LastName,MiddleName,Gender,DateOfBirth,ContactNumber,Email,Address,Department,Position,DateHired,EmploymentStatus,EmploymentType,BasicSalary,SSSNumber,PhilHealthNumber,PagIBIGNumber,TINNumber,DateCreated,DateUpdated")] Employee employee)
    {
        if (employeeid != employee.EmployeeID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                    employee.DateUpdated = DateTime.Now;
                _context.Update(employee);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(employee.EmployeeID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "Name", "Name", employee.Department);
            ViewBag.Positions = new SelectList(await _context.Positions.OrderBy(p => p.Name).ToListAsync(), "Name", "Name", employee.Position);
            return View(employee);
    }

    // GET: EMPLOYEES/Delete/5
    public async Task<IActionResult> Delete(int? employeeid)
    {
        if (employeeid == null)
        {
            return NotFound();
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(m => m.EmployeeID == employeeid);
        if (employee == null)
        {
            return NotFound();
        }

        return View(employee);
    }

    // POST: EMPLOYEES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? employeeid)
    {
        var employee = await _context.Employees.FindAsync(employeeid);
        if (employee != null)
        {
            _context.Employees.Remove(employee);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool EmployeeExists(int? employeeid)
    {
        return _context.Employees.Any(e => e.EmployeeID == employeeid);
    }
  }
}
