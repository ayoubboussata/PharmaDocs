FROM python:3.12-slim

WORKDIR /app

# Deps eerst → laag wordt gecachet zolang requirements niet wijzigen.
COPY requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt

COPY app ./app
COPY prompts ./prompts

# Sleutels (ANTHROPIC_API_KEY, VOYAGE_API_KEY) komen als omgevingsvariabelen
# van Container Apps, niet uit een .env in de image.
EXPOSE 8000

# Niet als root draaien: een aparte non-root gebruiker aanmaken en overschakelen.
# Poort 8000 (> 1024) is zonder rootrechten te binden.
RUN useradd --create-home --uid 1001 appuser
USER appuser

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
