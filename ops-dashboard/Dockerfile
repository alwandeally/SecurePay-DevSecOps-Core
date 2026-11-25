FROM nginxinc/nginx-unprivileged:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY index.html ops.css /usr/share/nginx/html/
COPY src/ /usr/share/nginx/html/src/
USER 101:101
EXPOSE 8080
