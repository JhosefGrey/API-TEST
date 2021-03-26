using System;
using API_TEST.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon;

namespace API_TEST.Controllers
{

    [Route("api/[controller]")]
    public class ValuesController : ControllerBase
    {

        public string accessKey = "";
        public string secretKey = "";

        private readonly IAmazonS3 amazons3;

        public ValuesController(IAmazonS3 amazonS3)
        {
            this.amazons3 = amazonS3;
        }

        private string connection = "";






        [HttpGet]
        [Route("/test")]
        public string GetTest()
        {
            return "Test de que si reconoce el controlador";
        }


        [HttpGet]
        public IActionResult GetContactos()
        {
            IEnumerable<Models.Contacto> lContactos = null;
            using (var db = new MySqlConnection(connection))
            {
                var sql = "SELECT id,nombre,apellido,telefono FROM Contacto";
                lContactos = db.Query<Contacto>(sql);

            }
            return Ok(lContactos);

        }

        [HttpPost]
        public IActionResult AddContacto([FromBody] Models.Contacto contacto)
        {
            int result = 0;
            string msg = "";
            using (var db = new MySqlConnection(connection))
            {
                var sql = "INSERT INTO Contacto(nombre,apellido,telefono) values(@nombre, @apellido, @telefono)";
                if (contacto.nombre.Length <= 0 && contacto.telefono.Length <= 0 || contacto.nombre == null && contacto.telefono == null)
                {
                    msg = "No se puede guardar un contacto sin nombre y/o telefono";
                }
                else
                {
                    result = db.Execute(sql, contacto);
                    if (result >= 1)
                    {
                        msg = "Se ha guardo el contacto en la base de datos";
                    }
                    else
                    {
                        msg = "NO se ha podido guardar el contacto";
                    }
                }

            }
            return Ok(msg);
        }


        [HttpPut]
        public IActionResult editContacto([FromBody] Models.Contacto contacto)
        {
            int result = 0;
            string msg = "";
            using (var db = new MySqlConnection(connection))
            {
                if (contacto.id == 0 || contacto.id.ToString() == null)
                {
                    return NotFound();
                }
                var sql = "UPDATE Contacto SET nombre=@nombre, apellido=@apellido, telefono=@telefono WHERE id=@id";
                if (contacto.nombre.Length <= 0 && contacto.telefono.Length <= 0 || contacto.nombre == null && contacto.telefono == null)
                {
                    msg = "No se puede actualizar un contacto sin nombre y/o telefono";
                }
                else
                {
                    result = db.Execute(sql, contacto);
                    if (result >= 1)
                    {
                        msg = "Se ha actualizado el contacto en la base de datos";
                    }
                    else
                    {
                        msg = "NO se ha podido actualizar el contacto";
                    }
                }
            }

            return Ok(msg);
        }


        [HttpDelete]
        public IActionResult elimiarContacto([FromBody] Models.Contacto contacto)
        {
            int result = 0;
            string msg = "";
            using (var db = new MySqlConnection(connection))
            {
                if (contacto.id.ToString().Length > 0 || contacto.id.ToString() != null)
                {
                    var sql = "DELETE FROM Contacto WHERE id=@id";
                    result = db.Execute(sql, contacto);
                    if (result >= 1)
                    {
                        msg = "Se ha borrado el contacto en la base de datos";
                    }
                    else
                    {
                        msg = "NO se ha podido borrar el contacto";
                    }
                }
                else
                {
                    msg = "No se puede eliminar un contanto sin id";
                }
            }
            return Ok(msg);
        }

        [HttpPost]
        [Route("/api/values/imagen")]
        public async Task<IActionResult> uploadImage([FromForm] IFormFile file)
        {
            if (file == null || file.Length <= 0)
            {
                return Ok("Por favor envie un archivo");
            }
            if (file.Length >= 4000000)
            {
                return Ok("El archivo es muy grande por favor subir archvios de un maximo de 3 Mg");
            }
            try
            {
                IAmazonS3 client;
                using (client = new AmazonS3Client(accessKey, secretKey, RegionEndpoint.USEast1))
                {

                    var putRequetes = new PutObjectRequest()
                    {
                        BucketName = "upladtestsiteexample",
                        Key = file.FileName,
                        InputStream = file.OpenReadStream(),
                        ContentType = file.ContentType
                    };

                    var result = await client.PutObjectAsync(putRequetes);
                  
                    return Ok("Se ha subido exitosamente el archivo a s3");
                }
            }
            catch (Exception)
            {

                throw;
            }


        }

        static readonly string htmlBody = @"<html>
<head></head>
<body>
  <h1>Amazon SES Test (AWS SDK para .NET)</h1>
  <p>This email was sent with
    <a href='https://aws.amazon.com/ses/'>Amazon SES</a> using the
    <a href='https://aws.amazon.com/sdk-for-net/'>
      AWS SDK para .NET</a>.</p>
</body>
</html>";
        [HttpPost]
        [Route("/api/values/correo")]
        public async Task<IActionResult> sendEmail([FromForm]string para, string asunto)
        {
            if (para.Length <= 0)
            {
                return Ok("Por favor ingrese una direccion de correo");    
            }

            if (asunto.Length <= 0)
            {
                return Ok("Por favor ingrese un asunto ");
            }

            using (var ses = new AmazonSimpleEmailServiceClient(accessKey, secretKey, RegionEndpoint.USEast1))
            {
                var sendResult = new SendEmailRequest
                {
                    Source = "wewom15875@naymeo.com",
                    Destination = new Destination
                    {
                        ToAddresses = new List<string> { para }
                    },
                    Message = new Message
                    {
                        Subject = new Content(asunto),
                        Body = new Body
                        {
                            Html = new Content
                            {
                                Charset = "UTF-8",
                                Data = htmlBody
                            }
                        }
                    }
                };
                try
                {
                    var response = await ses.SendEmailAsync(sendResult);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error message: " + ex.Message);
                }
            }
            return Ok("Se ha enviado el correo exitosamente");
        }

       
    

    }
}
